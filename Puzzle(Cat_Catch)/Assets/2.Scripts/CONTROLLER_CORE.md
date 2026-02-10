# 컨트롤러 핵심 코드 요약

정리 후 **꼭 필요한 동작**만 남긴 흐름입니다.

---

## 1. 진입점 (StageController)

- **Start**: InputManager 생성 → 스테이지 빌드 → ActionManager 생성 → ComposeStage → 카메라 맞춤
- **Update**:
  - **누름**: `touch2BoardPosition`이 보드 안 + 스와이프 가능 블록 위 → `m_DownPos`, `m_DownPoint` 저장
  - **뗌**: `EvalSwipeDir(시작, 끝)` 로 방향 구해서 `Swipe.NA` 아니면 `DoSwipeAction(row, col, dir)` 한 번 호출

핵심: **누른 블록 (row, col)** + **스와이프 방향**만 ActionManager에 넘김.

---

## 2. 입력 → 보드 좌표 (InputManager)

- **isTouchDown / isTouchUp**: Handler(마우스 또는 터치)에서 한 프레임만 true
- **touch2BoardPosition**: `ScreenToWorldPoint` → `InverseTransformPoint(container)` → 보드 로컬
- **EvalSwipeDir(start, end)**: TouchEvaluator에 위임

핵심: **스크린 좌표를 보드 로컬로 변환** + **스와이프 방향 계산**.

---

## 3. 스와이프 방향 (TouchEvaluator)

- **Swipe**: NA, RIGHT, UP, LEFT, DOWN
- **EvalSwipeDir**: 드래그 거리 ≤ 0.2 → NA. 그 외는 각도를 90° 구간으로 나눠 방향 반환
- **GetTargetRow / GetTargetCol**: 스와이프 시 인접 칸 row/col 오프셋

핵심: **짧은 드래그 무시** + **45° 기준 4방향**.

---

## 4. 스와이프 실행 (ActionManager)

- **DoSwipeAction(row, col, dir)**: `IsValideSwipe` 통과 시에만 `CoSwipe` 코루틴 시작
- **CoSwipe**:
  1. `Stage.CoDoSwipeAction` → 두 블록 교환(애니 + 배열 갱신)
  2. 교환 성공 시 `CoEvaluateBoard` (매치 찾기 → 제거 → 드롭/스폰 반복)
  3. 매치가 하나도 없으면 같은 스와이프로 `CoDoSwipeAction` 한 번 더 호출 → **되돌리기**
- **m_Running**: 동시에 두 번 스와이프 안 되도록 플래그

핵심: **스와이프 → 평가 → 매치 없으면 되돌리기**.

---

## 5. 블록 좌표 판정 (Stage)

- **LocalToGrid(local, out row, out col)**: 보드 로컬 → (row, col). `IsInsideBoard` / `IsOnValideBlock`에서 공통 사용
- **IsInsideBoard**: LocalToGrid 후 0 ≤ row < maxRow, 0 ≤ col < maxCol
- **IsOnValideBlock**: LocalToGrid 후 `board.IsSwipeable(row, col)` + blockPos 반환
- **IsValideSwipe**: dir에 따라 보드 밖으로 나가는지만 검사

핵심: **로컬 좌표 → 그리드 인덱스** 한 곳에서 처리.

---

## 제거·간소화한 것

| 항목 | 변경 |
|------|------|
| InputManager | `touchPosition` 제거, `TouchToPosition` → `ToBoardPosition`, 주석 축소 |
| StageController | `m_bInit` 제거(Start에서 한 번만 실행), 변수명 짧게, 주석 축소 |
| ActionManager | `m_Container` 제거(MonoBehaviour만 보관), 코루틴/변수명 정리 |
| Stage | 좌표 변환을 `LocalToGrid`로 통합, `IsValideSwipe` 오타(;) 수정 |
| 핸들러/TouchEvaluator | 주석만 한 줄로, TouchHandler에 touchCount 체크 추가(예외 방지) |

동작은 그대로, **필요한 코드만 남기고** 정리된 상태입니다.
