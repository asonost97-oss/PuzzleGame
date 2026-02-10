# 스크립트 역할 분류 (Scripts Role Map)

프로젝트 스크립트를 **블럭 / 셀 / 스테이지 / 보드 / 컨트롤러** 5가지 역할로 나누고, 그 외는 **이벤트**로 정의했습니다.

---

## 1. 블럭 (Block)

**역할:** 한 칸 블록의 데이터, 연출, 타입 정의, 생성

| 스크립트 | 경로 | 설명 |
|----------|------|------|
| Block.cs | Board/Blocks/ | 블록 데이터·상태·매칭 로직 |
| BlockBehaviour.cs | Board/Blocks/ | 블록 GameObject 표시·제거 연출 |
| BlockActionBehaviour.cs | Board/Blocks/ | 블록 드롭(낙하) 애니메이션 |
| BlockDefine.cs | Board/Blocks/ | BlockType, BlockBreed, BlockStatus, BlockQuestType, BlockMethod |
| BlockFactory.cs | Board/Blocks/ | Block 인스턴스 생성 |

---

## 2. 셀 (Cell)

**역할:** 보드 한 칸(배경/타일)의 데이터, 연출, 타입 정의, 생성

| 스크립트 | 경로 | 설명 |
|----------|------|------|
| Cell.cs | Board/Cells/ | 셀 데이터·타입 |
| CellBehaviour.cs | Board/Cells/ | 셀 GameObject 표시 |
| CellDefine.cs | Board/Cells/ | CellType, CellTypeMethod |
| CellFactory.cs | Board/Cells/ | Cell 인스턴스 생성 |

---

## 3. 스테이지 (Stage)

**역할:** 한 판(스테이지) 구성, 보드 구성, 스와이프·평가·후처리

| 스크립트 | 경로 | 설명 |
|----------|------|------|
| Stage.cs | Stage/ | 한 스테이지 진입점, 보드 참조, 스와이프/평가/후처리 |
| StageBuilder.cs | Stage/ | 스테이지 데이터 로드, Stage/Board 초기 구성 |
| StageInfo.cs | Stage/ | 스테이지 JSON 데이터 구조 |
| StageReader.cs | Stage/ | Resources에서 스테이지 JSON 로드 |
| ActionManager.cs | Stage/ | 스와이프 액션·보드 평가 연쇄 처리(코루틴) |

---

## 4. 보드 (Board)

**역할:** 격자(셀/블록 배열), 매칭·셔플·드롭·스폰

| 스크립트 | 경로 | 설명 |
|----------|------|------|
| Board.cs | Board/ | 보드 핵심 로직(셀/블록 배열, 매칭, 셔플, 드롭, 스폰) |
| BoardEnumerator.cs | Board/ | 보드 순회·특수 셀 판정(케이지 등) |
| BoardShuffler.cs | Board/ | 보드 셔플(시작 시 3매치 없도록 재배치) |

---

## 5. 컨트롤러 (Controller) — 마우스 / 탭(터치)

**역할:** 입력(마우스 또는 터치) 수집, 보드 좌표 변환, 스와이프 방향 판정

| 스크립트 | 경로 | 설명 |
|----------|------|------|
| StageController.cs | Stage/ | 씬 진입점, 스테이지 빌드·입력 처리·ActionManager 연동 |
| InputManager.cs | Utils/InputEvent/ | 터치/마우스 → 보드 좌표 변환, 스와이프 방향 판정 |
| IInputHandlerBase.cs | Utils/InputEvent/ | 입력 추상화 인터페이스(한 번 누름/뗌/좌표) |
| MouseHandler.cs | Utils/InputEvent/ | 마우스 입력 구현(에디터·PC) |
| TouchHandler.cs | Utils/InputEvent/ | 터치 입력 구현(모바일) |
| TouchEvaluator.cs | Utils/InputEvent/ | Swipe 열거형, 스와이프 방향·각도 계산 |

---

## 6. 이벤트 (Event) — 그 외 공통/유틸/이펙트

**역할:** 입력·연출·데이터 공통 요소, 이벤트성 유틸, 이펙트

| 스크립트 | 경로 | 설명 |
|----------|------|------|
| **입력·연출 보조** | | |
| BlockPos.cs | Utils/ | 블록 인덱스(row, col) 구조체 |
| Returnable.cs | Utils/ | 코루틴 결과 전달용 래퍼 |
| Action2D.cs | Utils/ | 2D 이동·스케일 애니메이션 코루틴 |
| **공통 상수·설정** | | |
| Constants.cs | Core/ | BLOCK_ORG, SWIPE_DURATION, BLOCK_DESTROY_SCALE |
| BlockConfig.cs | Scriptable/ | 블록 스프라이트·폭발 이펙트 등 ScriptableObject |
| **매치/퀘스트 정의** | | |
| QuestDefine.cs | Quest/ | MatchType, MatchTypeMethod(매치 조합) |
| **이펙트** | | |
| ParticleAutoDestroy.cs | Effect/ | 파티클 재생 종료 시 GameObject 자동 제거 |
| **기타 유틸** | | |
| useful.cs | Utils/ | SortedListMethods(First 확장) |
| CameraAgent.cs | Core/ | 보드 단위에 맞춘 카메라 뷰 설정 |

---

## 폴더 구조 요약

```
2.Scripts/
├── Board/           ← 보드 + 블럭/셀 (역할 1, 2, 4)
│   ├── Board.cs, BoardEnumerator.cs, BoardShuffler.cs
│   ├── Blocks/      (Block, BlockBehaviour, BlockActionBehaviour, BlockDefine, BlockFactory)
│   └── Cells/       (Cell, CellBehaviour, CellDefine, CellFactory)
├── Stage/           ← 스테이지 + 컨트롤러 진입점 (역할 3, 5 일부)
│   ├── Stage.cs, StageBuilder.cs, StageInfo.cs, StageReader.cs, ActionManager.cs
│   └── StageController.cs   ← 컨트롤러(씬 진입)
├── Utils/
│   └── InputEvent/  ← 컨트롤러(마우스/탭) (역할 5)
│       (InputManager, IInputHandlerBase, MouseHandler, TouchHandler, TouchEvaluator)
├── Core/            ← 이벤트: 상수, 카메라
├── Effect/          ← 이벤트: 파티클
├── Quest/           ← 이벤트: 매치 타입
├── Scriptable/      ← 이벤트: 블록 설정 에셋
└── Utils/           ← 이벤트: BlockPos, Returnable, Action2D, useful
```

---

## 역할별 의존 관계 요약

- **블럭:** Board(배열), BlockConfig(연출), Constants(연출 수치)
- **셀:** StageInfo(스테이지 데이터)
- **스테이지:** Board, Block, Cell, StageBuilder, ActionManager
- **보드:** Block, Cell, BoardEnumerator, BoardShuffler, StageBuilder
- **컨트롤러:** Stage, InputManager, BlockPos, Swipe/TouchEvaluator
- **이벤트:** 위 역할들에서 참조되는 공통 타입·유틸·이펙트

이 문서는 `Assets/2.Scripts/SCRIPTS_ROLE_MAP.md` 에 저장되어 있습니다.
