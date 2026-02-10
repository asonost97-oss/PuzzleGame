// =============================================================================
// Cell.cs — 셀 타입 정의, 데이터, 표시, 생성 (통합)
// =============================================================================

using UnityEngine;

// -----------------------------------------------------------------------------
// [1] 셀 타입 정의 — 보드 칸 종류 + 블록 배치/이동 가능 여부
// -----------------------------------------------------------------------------
public enum CellType
{
    EMPTY = 0,   // 빈칸, 블록 배치·이동 불가
    BASIC = 1,   // 기본 칸 (배치·이동 가능)
    FIXTURE = 2,
    JELLY = 3,
}

// 역할: 셀 타입으로 블록 배치 가능/이동 가능 판정 (Board, StageBuilder에서 사용)
static class CellTypeMethod
{
    public static bool IsBlockAllocatableType(this CellType t) => t != CellType.EMPTY;
    public static bool IsBlockMovableType(this CellType t) => t != CellType.EMPTY;
}

// -----------------------------------------------------------------------------
// [2] 셀 데이터 — 타입 + GameObject(CellManager 또는 CellBehaviour) 연결
// -----------------------------------------------------------------------------
public class Cell
{
    protected CellType m_CellType;
    public CellType type { get => m_CellType; set => m_CellType = value; }

    protected CellManager m_CellManager;
    protected CellBehaviour m_CellBehaviour;

    public Cell(CellType cellType) => m_CellType = cellType;

    // 역할: 프리팹으로 셀 GameObject 생성. CellManager 우선, 없으면 CellBehaviour 사용
    public Cell InstantiateCellObj(GameObject cellPrefab, Transform containerObj)
    {
        var go = Object.Instantiate(cellPrefab, Vector3.zero, Quaternion.identity);
        go.transform.parent = containerObj;
        var manager = go.GetComponent<CellManager>();
        if (manager != null)
        {
            m_CellManager = manager;
            m_CellBehaviour = null;
            manager.SetCell(this);
            return this;
        }
        var behaviour = go.GetComponent<CellBehaviour>();
        if (behaviour != null)
        {
            m_CellManager = null;
            m_CellBehaviour = behaviour;
            behaviour.SetCell(this);
            return this;
        }
        Debug.LogError($"Cell 프리팹에 CellManager 또는 CellBehaviour 스크립트가 없습니다. 프리팹: {cellPrefab.name}");
        return this;
    }

    public void Move(float x, float y)
    {
        var t = m_CellManager != null ? m_CellManager.transform : m_CellBehaviour?.transform;
        if (t != null) t.position = new Vector3(x, y);
    }

    // 역할: 장애물(빈칸)이면 true. Board 매칭/드롭 시 "막힌 칸" 판정에 사용
    public bool IsObstracle() => type == CellType.EMPTY;
}

// -----------------------------------------------------------------------------
// [3] 셀 표시 — Cell과 1:1 연결, 스프라이트·스케일
// -----------------------------------------------------------------------------
public class CellBehaviour : MonoBehaviour
{
    Cell m_Cell;
    SpriteRenderer m_SpriteRenderer;

    void Start()
    {
        m_SpriteRenderer = GetComponent<SpriteRenderer>();
        UpdateView(false);
        FitToCellSize(1f);
    }

    // 역할: 셀을 지정 월드 크기(cellSize)에 맞춰 스케일
    public void FitToCellSize(float cellSize)
    {
        if (m_SpriteRenderer == null || m_SpriteRenderer.sprite == null) return;
        var size = m_SpriteRenderer.sprite.bounds.size;
        if (size.x <= 0 || size.y <= 0) return;
        transform.localScale = new Vector3(cellSize / size.x, cellSize / size.y, 1f);
    }

    public void SetCell(Cell cell) => m_Cell = cell;

    public void UpdateView(bool bValueChanged)
    {
        if (m_Cell.type == CellType.EMPTY) m_SpriteRenderer.sprite = null;
    }
}

// -----------------------------------------------------------------------------
// [4] 셀 생성 — StageInfo (row,col) → CellType → Cell 인스턴스
// -----------------------------------------------------------------------------
public static class CellFactory
{
    // 역할: 스테이지 데이터에서 (row,col) 셀 타입을 읽어 해당 타입의 Cell 생성
    public static Cell SpawnCell(StageInfo stageInfo, int nRow, int nCol)
    {
        Debug.Assert(stageInfo != null && nRow < stageInfo.row && nCol < stageInfo.col);
        return SpawnCell(stageInfo.GetCellType(nRow, nCol));
    }

    public static Cell SpawnCell(CellType cellType) => new Cell(cellType);
}
