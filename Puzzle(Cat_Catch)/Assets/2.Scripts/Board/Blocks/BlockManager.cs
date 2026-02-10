// =============================================================================
// BlockManager.cs — Block/Cat 프리팹용 매니저 (인스펙터에서 필요한 것 할당)
// =============================================================================
// 블록(고양이) 프리팹에 이 스크립트를 붙이고, Block Config·Sprite Renderer 등을 할당하세요.
// 표시·제거 연출·드롭(낙하) 연출을 한 스크립트에서 처리합니다.
// =============================================================================

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlockManager : MonoBehaviour
{
    [Header("블록 설정")]
    [Tooltip("블록 스프라이트·색·폭발 이펙트·드롭 속도 (Resources/BlockConfig 등)")]
    [SerializeField] BlockConfig m_BlockConfig;

    [Header("표시 (선택)")]
    [Tooltip("비워두면 같은 오브젝트에서 GetComponent로 찾습니다")]
    [SerializeField] SpriteRenderer m_SpriteRenderer;

    Block m_Block;
    public bool isMoving { get; set; }
    Queue<Vector3> m_MovementQueue = new Queue<Vector3>();

    void Start()
    {
        if (m_SpriteRenderer == null) m_SpriteRenderer = GetComponent<SpriteRenderer>();
        UpdateView(false);
        FitToCellSize(1f);
    }

    /// <summary>Block 데이터와 연결 (Board에서 호출)</summary>
    public void SetBlock(Block block) => m_Block = block;

    /// <summary>블록을 지정 월드 크기(cellSize)에 맞춰 스케일</summary>
    public void FitToCellSize(float cellSize)
    {
        if (m_SpriteRenderer == null || m_SpriteRenderer.sprite == null) return;
        var size = m_SpriteRenderer.sprite.bounds.size;
        if (size.x <= 0 || size.y <= 0) return;
        transform.localScale = new Vector3(cellSize / size.x, cellSize / size.y, 1f);
    }

    /// <summary>breed에 맞는 스프라이트 갱신</summary>
    public void UpdateView(bool bValueChanged)
    {
        if (m_Block == null || m_SpriteRenderer == null) return;
        if (m_Block.type == BlockType.EMPTY) m_SpriteRenderer.sprite = null;
        else if (m_Block.type == BlockType.BASIC && m_BlockConfig != null)
            m_SpriteRenderer.sprite = m_BlockConfig.basicBlockSprites[(int)m_Block.breed];
    }

    /// <summary>블록 제거 연출 (축소 + 폭발 파티클) 후 GameObject 제거</summary>
    public void DoActionClear() => StartCoroutine(CoStartSimpleExplosion(true));

    IEnumerator CoStartSimpleExplosion(bool bDestroy = true)
    {
        yield return Action2D.Scale(transform, Constants.BLOCK_DESTROY_SCALE, 4f);
        if (m_BlockConfig == null) { if (bDestroy) Destroy(gameObject); yield break; }
        var explosionObj = m_BlockConfig.GetExplosionObject(BlockQuestType.CLEAR_SIMPLE);
        var main = explosionObj.GetComponent<ParticleSystem>().main;
        main.startColor = m_BlockConfig.GetBlockColor(m_Block.breed);
        explosionObj.SetActive(true);
        explosionObj.transform.position = transform.position;
        yield return new WaitForSeconds(0.1f);
        if (bDestroy) Destroy(gameObject);
    }

    /// <summary>드롭(낙하) 거리 추가 — Board에서 호출</summary>
    public void MoveDrop(Vector2 vtDropDistance)
    {
        m_MovementQueue.Enqueue(new Vector3(vtDropDistance.x, vtDropDistance.y, 1));
        if (!isMoving) StartCoroutine(DoActionMoveDrop());
    }

    IEnumerator DoActionMoveDrop(float acc = 1.0f)
    {
        isMoving = true;
        while (m_MovementQueue.Count > 0)
        {
            var dest = m_MovementQueue.Dequeue();
            int dropIndex = Mathf.Clamp((int)Mathf.Abs(dest.y), 1, 9);
            float duration = m_BlockConfig != null ? m_BlockConfig.dropSpeed[dropIndex - 1] : 0.2f;
            yield return CoStartDropSmooth(dest, duration * acc);
        }
        isMoving = false;
    }

    IEnumerator CoStartDropSmooth(Vector2 vtDropDistance, float duration)
    {
        var to = new Vector2(transform.position.x + vtDropDistance.x, transform.position.y - vtDropDistance.y);
        yield return Action2D.MoveTo(transform, to, duration);
    }
}
