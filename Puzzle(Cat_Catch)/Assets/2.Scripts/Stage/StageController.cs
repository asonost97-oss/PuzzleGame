// =============================================================================
// StageController.cs — 터치/마우스 입력 → 보드 좌표 → 스와이프 → 블록 교환 (통합)
// =============================================================================

using System.Collections;
using UnityEngine;

// -----------------------------------------------------------------------------
// [1] 보드 칸 인덱스 — 터치한 블록의 (row, col) 전달용
// -----------------------------------------------------------------------------
public struct BlockPos
{
    public int row { get; set; }
    public int col { get; set; }
    public BlockPos(int r = 0, int c = 0) { row = r; col = c; }
    public override bool Equals(object obj) => obj is BlockPos p && row == p.row && col == p.col;
    public override int GetHashCode() => (row, col).GetHashCode();
    public override string ToString() => $"(row={row}, col={col})";
}

// -----------------------------------------------------------------------------
// [2] 스와이프 방향 — 드래그를 4방향(UP/DOWN/LEFT/RIGHT) 또는 NA로 구분
// -----------------------------------------------------------------------------
public enum Swipe { NA = -1, RIGHT = 0, UP = 1, LEFT = 2, DOWN = 3 }

public static class SwipeDirMethod
{
    // 역할: 스와이프 방향 → 인접 칸 row/col 오프셋 (Stage에서 교환 대상 칸 계산에 사용)
    public static int GetTargetRow(this Swipe d) => d == Swipe.DOWN ? -1 : d == Swipe.UP ? 1 : 0;
    public static int GetTargetCol(this Swipe d) => d == Swipe.LEFT ? -1 : d == Swipe.RIGHT ? 1 : 0;
}

public static class TouchEvaluator
{
    const float MinDrag = 0.2f;

    // 역할: 드래그 시작·끝 좌표 → 각도 계산 → 90° 구간으로 방향 반환. 짧은 드래그(0.2 이하)는 NA
    public static Swipe EvalSwipeDir(Vector2 start, Vector2 end)
    {
        float angle = Angle(start, end);
        if (angle < 0) return Swipe.NA;
        int i = (((int)angle + 45) % 360) / 90;
        return i switch { 0 => Swipe.RIGHT, 1 => Swipe.UP, 2 => Swipe.LEFT, 3 => Swipe.DOWN, _ => Swipe.NA };
    }

    static float Angle(Vector2 start, Vector2 end)
    {
        var d = end - start;
        if (d.magnitude <= MinDrag) return -1f;
        float rad = Mathf.Atan2(d.y, d.x);
        if (rad < 0) rad += Mathf.PI * 2;
        return rad * Mathf.Rad2Deg;
    }
}

// -----------------------------------------------------------------------------
// [3] 입력 추상화 — 마우스/터치를 동일한 인터페이스로 제공 (플랫폼 분기 제거)
// -----------------------------------------------------------------------------
public interface IInputHandlerBase
{
    bool isInputDown { get; }
    bool isInputUp { get; }
    Vector2 inputPosition { get; }
}

// 역할: PC·에디터용. Fire1(보통 왼쪽 클릭) 누름/뗌 + 마우스 스크린 좌표
public class MouseHandler : IInputHandlerBase
{
    bool IInputHandlerBase.isInputDown => Input.GetButtonDown("Fire1");
    bool IInputHandlerBase.isInputUp => Input.GetButtonUp("Fire1");
    Vector2 IInputHandlerBase.inputPosition => Input.mousePosition;
}

// 역할: 모바일용. 첫 번째 터치 Began/Ended + 터치 스크린 좌표 (touchCount 체크로 예외 방지)
public class TouchHandler : IInputHandlerBase
{
    bool IInputHandlerBase.isInputDown => Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began;
    bool IInputHandlerBase.isInputUp => Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Ended;
    Vector2 IInputHandlerBase.inputPosition => Input.touchCount > 0 ? Input.GetTouch(0).position : Vector2.zero;
}

// -----------------------------------------------------------------------------
// [4] 입력 매니저 — 스크린 좌표 → 보드(컨테이너) 로컬 좌표 변환 + 스와이프 방향
// -----------------------------------------------------------------------------
public class InputManager
{
    readonly Transform m_Container;
#if UNITY_ANDROID && !UNITY_EDITOR
    readonly IInputHandlerBase m_Handler = new TouchHandler();
#else
    readonly IInputHandlerBase m_Handler = new MouseHandler();
#endif

    public InputManager(Transform container) => m_Container = container;

    // 역할: 한 프레임만 true. "누르기 시작" / "뗌" 감지
    public bool isTouchDown => m_Handler.isInputDown;
    public bool isTouchUp => m_Handler.isInputUp;

    // 역할: 터치/마우스 위치를 보드 기준 로컬 좌표로 변환 → Stage에서 (row,col) 계산에 사용
    public Vector2 touch2BoardPosition => ToBoardPosition(m_Handler.inputPosition);

    public Swipe EvalSwipeDir(Vector2 start, Vector2 end) => TouchEvaluator.EvalSwipeDir(start, end);

    // 역할: 스크린 픽셀 → 월드 → 컨테이너 로컬 (보드가 컨테이너 자식이므로 보드 좌표계와 일치)
    Vector2 ToBoardPosition(Vector3 screenPos)
    {
        var world = Camera.main.ScreenToWorldPoint(screenPos);
        return m_Container.InverseTransformPoint(world);
    }
}

// -----------------------------------------------------------------------------
// [5] 액션 매니저 — 스와이프 실행 → 3매치 평가 → 매치 없으면 같은 스와이프로 되돌리기
// -----------------------------------------------------------------------------
public class ActionManager
{
    readonly Stage m_Stage;
    readonly MonoBehaviour m_Runner;
    bool m_Running;

    public ActionManager(Transform container, Stage stage)
    {
        m_Stage = stage;
        m_Runner = container.GetComponent<MonoBehaviour>();
    }

    // 역할: 유효한 스와이프만 코루틴으로 실행 (보드 밖으로 나가는 방향은 거부)
    public void DoSwipeAction(int row, int col, Swipe dir)
    {
        if (!m_Stage.IsValideSwipe(row, col, dir)) return;
        m_Runner.StartCoroutine(CoSwipe(row, col, dir));
    }

    // 역할: 1) 두 블록 교환 2) 연쇄 매치 평가·제거·드롭 3) 매치가 없었으면 같은 스와이프로 다시 교환(되돌리기)
    IEnumerator CoSwipe(int row, int col, Swipe dir)
    {
        if (m_Running) yield break;
        m_Running = true;

        var swapped = new Returnable<bool>(false);
        yield return m_Stage.CoDoSwipeAction(row, col, dir, swapped);

        if (swapped.value)
        {
            var matched = new Returnable<bool>(false);
            yield return CoEvaluateBoard(matched);
            if (!matched.value)
                yield return m_Stage.CoDoSwipeAction(row, col, dir, swapped);
        }

        m_Running = false;
    }

    // 역할: 매치가 있는 한 반복: Evaluate(3매치 찾아 제거) → Postprocess(드롭·스폰)
    IEnumerator CoEvaluateBoard(Returnable<bool> anyMatch)
    {
        while (true)
        {
            var match = new Returnable<bool>(false);
            yield return m_Runner.StartCoroutine(m_Stage.Evaluate(match));
            if (!match.value) break;
            anyMatch.value = true;
            yield return m_Runner.StartCoroutine(m_Stage.PostprocessAfterEvaluate());
        }
    }
}

// -----------------------------------------------------------------------------
// [6] 스테이지 컨트롤러 (씬 진입점) — 초기화 + 매 프레임 입력 처리 → 스와이프만 ActionManager에 전달
// -----------------------------------------------------------------------------
public class StageController : MonoBehaviour
{
    Stage m_Stage;
    InputManager m_Input;
    ActionManager m_Action;
    bool m_TouchDown;
    BlockPos m_DownPos;
    Vector2 m_DownPoint;

    [SerializeField] Transform m_Container;
    [SerializeField] GameObject m_CellPrefab;
    [SerializeField] GameObject m_BlockPrefab;
    [SerializeField] Camera m_StageCamera;

    void Start()
    {
        m_Input = new InputManager(m_Container);
        m_Stage = StageBuilder.BuildStage(1);
        m_Action = new ActionManager(m_Container, m_Stage);
        m_Stage.ComposeStage(m_CellPrefab, m_BlockPrefab, m_Container); ////////////////////
        FitCamera();
    }

    // 역할: 누름 → 보드 안 + 스와이프 가능 블록 위일 때만 (row,col)과 시작 좌표 저장 / 뗌 → 스와이프 방향 계산 후 DoSwipeAction 한 번 호출
    void Update()
    {
        if (!m_TouchDown && m_Input.isTouchDown)
        {
            var p = m_Input.touch2BoardPosition;
            if (m_Stage.IsInsideBoard(p) && m_Stage.IsOnValideBlock(p, out var pos))
            {
                m_TouchDown = true;
                m_DownPos = pos;
                m_DownPoint = p;
            }
        }
        else if (m_TouchDown && m_Input.isTouchUp)
        {
            var dir = m_Input.EvalSwipeDir(m_DownPoint, m_Input.touch2BoardPosition);
            if (dir != Swipe.NA)
                m_Action.DoSwipeAction(m_DownPos.row, m_DownPos.col, dir);
            m_TouchDown = false;
        }
    }

    void FitCamera()
    {
        var cam = m_StageCamera != null ? m_StageCamera : Camera.main;
        if (cam == null || !cam.orthographic) return;
        float h = m_Stage.maxRow, w = m_Stage.maxCol;
        cam.orthographicSize = Mathf.Max(h * 0.5f, w * 0.5f / cam.aspect);
    }
}
