# 컨트롤러 동작 흐름 (Controller Flow)

컨트롤러가 **입력 인식 → 보드 좌표 변환 → 스와이프 판정 → 블록 교환**까지 어떻게 이어지는지 단계별로 정리했습니다.

---

## 1. 전체 구조

```
[사용자 입력]
     ↓
MouseHandler / TouchHandler  ← IInputHandlerBase (누름/뗌/스크린 좌표)
     ↓
InputManager  ← 스크린 → 보드(컨테이너) 좌표 변환, 스와이프 방향 계산
     ↓
StageController  ← 매 프레임 Update()에서 입력 처리, 유효하면 ActionManager 호출
     ↓
ActionManager  ← 코루틴으로 스와이프 실행, 매치 없으면 되돌리기
     ↓
Stage.CoDoSwipeAction  ← 두 블록 교환(애니메이션 + 배열 갱신)
     ↓
Block.MoveTo  ← 실제 GameObject 이동 (Action2D.MoveTo 코루틴)
```

- **StageController**: 씬에 하나만 두는 MonoBehaviour. `Update()`에서만 입력을 보고, “누른 블록”과 “뗀 위치”로 스와이프를 판정한 뒤 **ActionManager**에만 넘깁니다.
- **InputManager**: 어떤 기기(마우스/터치)를 쓰든 **보드 기준 좌표**와 **스와이프 방향**만 제공합니다.
- **ActionManager**: 스와이프 실행, 보드 평가, 매치 없으면 **같은 스와이프로 되돌리기**까지 한 번에 처리합니다.

---

## 2. 입력이 어떻게 인식되는지

### 2.1 플랫폼별 핸들러

| 환경 | 사용 클래스 | 조건 (InputManager.cs) |
|------|-------------|--------------------------|
| 에디터 / PC | **MouseHandler** | `#else` (UNITY_ANDROID가 아니거나 에디터) |
| Android 빌드 | **TouchHandler** | `#if UNITY_ANDROID && !UNITY_EDITOR` |

둘 다 **IInputHandlerBase** 인터페이스로 같은 3가지만 제공합니다.

- `isInputDown`: **한 프레임만 true** (누르기 시작)
- `isInputUp`: **한 프레임만 true** (손/버튼 뗌)
- `inputPosition`: **현재 입력 위치 (스크린 픽셀 좌표)**

**MouseHandler**

- `Input.GetButtonDown("Fire1")` → 보통 마우스 왼쪽 클릭
- `Input.GetButtonUp("Fire1")` → 왼쪽 버튼 뗌
- `Input.mousePosition` → 스크린 좌표

**TouchHandler**

- `Input.GetTouch(0).phase == TouchPhase.Began` → 첫 번째 터치 시작
- `Input.GetTouch(0).phase == TouchPhase.Ended` → 첫 번째 터치 끝
- `Input.GetTouch(0).position` → 터치 스크린 좌표

즉, **“한 번 누름” / “한 번 뗌” / “그때의 좌표”**만 구분하고, 기기 차이는 InputManager 밖으로 나오지 않습니다.

---

## 3. 터치/클릭 위치가 “어느 블록”인지 어떻게 아는지

### 3.1 좌표 변환 경로

1. **스크린 (픽셀)**  
   - Mouse: `Input.mousePosition`  
   - Touch: `Input.GetTouch(0).position`

2. **InputManager.TouchToPosition()**
   - `Camera.main.ScreenToWorldPoint(vtInput)` → **월드 좌표**
   - `m_Container.InverseTransformPoint(vtMousePosW)` → **컨테이너 로컬 좌표**
   - 보드가 컨테이너 자식이므로, 이 좌표가 “보드 기준” 좌표가 됩니다.

3. **StageController**에서는 이걸 **touch2BoardPosition**으로 받아서 사용합니다.

### 3.2 보드 영역 안인지 (IsInsideBoard)

```csharp
// Stage.cs
// ptOrg = 컨테이너 로컬 좌표 (왼쪽 아래가 음수 쪽)
Vector2 point = new Vector2(ptOrg.x + (maxCol/2.0f), ptOrg.y + (maxRow/2.0f));
// point가 (0,0)~(maxCol, maxRow) 안에 있으면 보드 안
```

- 보드 중심을 (0,0)이 아니라 **칸 인덱스에 맞게** 오프셋을 더해, `point.x`, `point.y`가 각각 0~maxCol, 0~maxRow 범위인지 봅니다.

### 3.3 “스와이프 가능한 블록 위”인지 + 블록 인덱스 (IsOnValideBlock)

```csharp
// Stage.cs
Vector2 pos = new Vector2(point.x + (maxCol/2.0f), point.y + (maxRow/2.0f));
int nRow = (int)pos.y;   // 소수 버림 → 그리드 행
int nCol = (int)pos.x;   // 소수 버림 → 그리드 열
blockPos = new BlockPos(nRow, nCol);
return board.IsSwipeable(nRow, nCol);  // 해당 칸이 스와이프 가능한 셀인지
```

- 같은 오프셋으로 **정수 행/열 (nRow, nCol)** 을 구하고,
- 그 칸이 **스와이프 가능(빈칸/장애물 아님)** 이면 `blockPos`에 (row, col)을 넣고 true를 반환합니다.
- 그래서 **“인식되는 블록”** = 터치/클릭 위치를 보드 그리드로 나눈 **(row, col)** 한 칸입니다.

---

## 4. 스와이프 방향이 어떻게 정해지는지

### 4.1 사용하는 값

- **m_ClickPos**: 터치/클릭 **시작** 시점의 `touch2BoardPosition` (보드 로컬)
- **뗀 시점**의 `touch2BoardPosition`

StageController에서:

```csharp
Swipe swipeDir = m_InputManager.EvalSwipeDir(m_ClickPos, point);
```

### 4.2 TouchEvaluator.EvalSwipeDir

1. **거리**
   - `dragDirection = vtEnd - vtStart`
   - `magnitude <= 0.2f` 이면 **너무 짧은 드래그** → **Swipe.NA** (인식 안 함)

2. **각도**
   - `Mathf.Atan2(dragDirection.y, dragDirection.x)` → 라디안
   - 0~360도로 변환

3. **90도 구간으로 방향 결정**
   - `swipe = (((int)angle + 45) % 360) / 90`
   - 0 → RIGHT, 1 → UP, 2 → LEFT, 3 → DOWN

즉, **짧게 치면 NA**, **조금만 드래그해도 45도 기준으로 상/하/좌/우 중 하나**로 인식됩니다.

### 4.3 스와이프 방향 → “맞닿은 칸” (GetTargetRow / GetTargetCol)

- **Swipe.UP**   → row + 1  
- **Swipe.DOWN** → row - 1  
- **Swipe.RIGHT** → col + 1  
- **Swipe.LEFT** → col - 1  

Stage에서는 이 델타로 **스와이프 대상 칸 (nSwipeRow, nSwipeCol)** 을 구합니다.

---

## 5. 블록이 실제로 어떻게 움직이는지

### 5.1 StageController → ActionManager

- **누름**: 보드 안 + 스와이프 가능 블록 위일 때만 `m_bTouchDown = true`, `m_BlockDownPos`, `m_ClickPos` 저장.
- **뗌**: `EvalSwipeDir(m_ClickPos, point)` 로 방향 구한 뒤, **Swipe.NA가 아니면**
  - `m_ActionManager.DoSwipeAction(m_BlockDownPos.row, m_BlockDownPos.col, swipeDir)` 한 번만 호출.

### 5.2 ActionManager.DoSwipeAction

1. `IsValideSwipe(nRow, nCol, swipeDir)` 로 **보드 밖으로 나가는 스와이프**인지 확인.
2. **한 번에 하나의 액션만** 실행되도록 `m_bRunning`으로 중복 실행 방지.
3. **CoDoSwipeAction** 코루틴 실행:
   - `Stage.CoDoSwipeAction(nRow, nCol, swipeDir, bSwipedBlock)` 호출
   - **교환 성공**이면:
     - `EvaluateBoard` → 3매치 찾아서 제거, 드롭/스폰 반복 (연쇄)
     - 이번 스와이프로 **매치가 하나도 없으면**  
       → `m_Stage.CoDoSwipeAction(nRow, nCol, swipeDir, ...)` **한 번 더 호출**해서 **같은 스와이프로 되돌림** (블록 원위치).
   - 끝나면 `m_bRunning = false`.

### 5.3 Stage.CoDoSwipeAction (두 블록 교환)

1. **대상 칸 계산**  
   `nSwipeRow = nRow + swipeDir.GetTargetRow()`, `nSwipeCol = nCol + swipeDir.GetTargetCol()`

2. **스와이프 가능 여부**  
   `m_Board.IsSwipeable(nSwipeRow, nSwipeCol)` 그리고 두 블록 모두 존재하는지 확인.

3. **블록별 스와이프 가능 여부**  
   `targetBlock.IsSwipeable(baseBlock)` (현재는 항상 true에 가깝게 구현 가능).

4. **애니메이션**
   - `baseBlock.MoveTo(targetPos, Constants.SWIPE_DURATION)`  
   - `targetBlock.MoveTo(basePos, Constants.SWIPE_DURATION)`  
   → 두 블록의 **Transform**이 서로의 위치로 **SWIPE_DURATION(0.2초)** 동안 이동.

5. **코루틴이 0.2초 대기**  
   `yield return new WaitForSeconds(Constants.SWIPE_DURATION);`

6. **데이터 갱신**
   - `blocks[nRow, nCol] = targetBlock;`
   - `blocks[nSwipeRow, nSwipeCol] = baseBlock;`
   - `actionResult.value = true` → ActionManager가 “교환 성공”으로 인식.

### 5.4 Block.MoveTo → 실제 이동

- `Block.MoveTo(Vector3 to, float duration)`  
  → `BlockBehaviour`가 붙은 GameObject에서  
  → `StartCoroutine(Action2D.MoveTo(blockObj, to, duration))` 실행.

- **Action2D.MoveTo**
  - 현재 위치에서 `to`까지 **duration** 동안 `Vector2.Lerp`로 선형 보간.
  - 매 프레임 `target.transform.position` 갱신.

그래서 **“블록이 움직인다”** =  
Stage가 두 Block에 **MoveTo(상대 위치, 0.2초)** 를 호출하고,  
각 Block의 **BlockBehaviour**가 **Action2D.MoveTo** 코루틴으로 **Transform 위치**를 바꾸는 구조입니다.

---

## 6. 요약 표

| 단계 | 담당 | 하는 일 |
|------|------|----------|
| 입력 감지 | MouseHandler / TouchHandler | 누름/뗌 한 프레임 + 스크린 좌표 |
| 좌표 변환 | InputManager | 스크린 → 월드 → 컨테이너 로컬 (touch2BoardPosition) |
| 보드/블록 판정 | Stage | IsInsideBoard, IsOnValideBlock → (row, col), IsValideSwipe |
| 스와이프 방향 | TouchEvaluator | 드래그 각도·거리 → UP/DOWN/LEFT/RIGHT/NA |
| 스와이프 실행 | StageController → ActionManager | (row, col) + Swipe → Stage.CoDoSwipeAction |
| 블록 교환 | Stage.CoDoSwipeAction | 두 Block의 MoveTo 호출 + 배열 교체 |
| 실제 이동 | Block.MoveTo → Action2D.MoveTo | Transform 위치를 duration 동안 보간 |

이 문서는 `Assets/2.Scripts/CONTROLLER_FLOW.md`에 있습니다.
