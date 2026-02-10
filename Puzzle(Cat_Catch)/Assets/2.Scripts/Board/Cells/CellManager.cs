// =============================================================================
// CellManager.cs — Cell 프리팹용 매니저 (인스펙터에서 필요한 것 할당)
// =============================================================================
// Cell 프리팹에 이 스크립트를 붙이고, Sprite Renderer 등을 할당하세요.
// =============================================================================

using UnityEngine;

public class CellManager : MonoBehaviour
{
    [Header("셀 표시")]
    [Tooltip("비워두면 같은 오브젝트에서 GetComponent로 찾습니다")]
    [SerializeField] SpriteRenderer m_SpriteRenderer;

    Cell m_Cell;

    void Start()
    {
        if (m_SpriteRenderer == null) m_SpriteRenderer = GetComponent<SpriteRenderer>();
        UpdateView(false);
        FitToCellSize(1f);
    }

    /// <summary>Cell 데이터와 연결 (Board에서 호출)</summary>
    public void SetCell(Cell cell) => m_Cell = cell;

    /// <summary>셀을 지정 월드 크기(cellSize)에 맞춰 스케일</summary>
    public void FitToCellSize(float cellSize)
    {
        if (m_SpriteRenderer == null || m_SpriteRenderer.sprite == null) return;
        var size = m_SpriteRenderer.sprite.bounds.size;
        if (size.x <= 0 || size.y <= 0) return;
        transform.localScale = new Vector3(cellSize / size.x, cellSize / size.y, 1f);
    }

    /// <summary>Cell 타입에 맞게 스프라이트 갱신</summary>
    public void UpdateView(bool bValueChanged)
    {
        if (m_Cell == null || m_SpriteRenderer == null) return;
        if (m_Cell.type == CellType.EMPTY) m_SpriteRenderer.sprite = null;
    }
}
