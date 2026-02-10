// =============================================================================
// Block.cs — 블록 타입 정의, 데이터/로직, 표시, 드롭 연출, 생성 (통합)
// =============================================================================

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// -----------------------------------------------------------------------------
// [1] 블록 타입·상태 정의 — EMPTY/BASIC, breed, NORMAL/MATCH/CLEAR, 퀘스트 효과
// -----------------------------------------------------------------------------
public enum BlockType { EMPTY = 0, BASIC = 1 }

public enum BlockBreed
{
    NA = -1, BREED_0 = 0, BREED_1 = 1, BREED_2 = 2, BREED_3 = 3, BREED_4 = 4, BREED_5 = 5,
}

public enum BlockStatus { NORMAL, MATCH, CLEAR }

public enum BlockQuestType
{
    NONE = -1, CLEAR_SIMPLE = 0, CLEAR_HORZ = 1, CLEAR_VERT = 2, CLEAR_CIRCLE = 3,
    CLEAR_LAZER = 4, CLEAR_HORZ_BUFF = 5, CLEAR_VERT_BUFF = 6, CLEAR_CIRCLE_BUFF = 7, CLEAR_LAZER_BUFF = 8,
}

// 역할: null 체크 후 IsEqual 호출 — Board 매칭 검사 시 NullReference 방지
static class BlockMethod
{
    public static bool IsSafeEqual(this Block block, Block target) => block != null && block.IsEqual(target);
}

// -----------------------------------------------------------------------------
// [2] 블록 데이터·로직 — 상태, breed, 매칭, 평가, GameObject 연결
// -----------------------------------------------------------------------------
public class Block
{
    public BlockStatus status;
    public BlockQuestType questType;
    public MatchType match = MatchType.NONE;
    public short matchCount;

    BlockType m_BlockType;
    public BlockType type { get => m_BlockType; set => m_BlockType = value; }

    protected BlockBreed m_Breed;
    public BlockBreed breed
    {
        get => m_Breed;
        set { m_Breed = value; m_BlockManager?.UpdateView(true); m_BlockBehaviour?.UpdateView(true); }
    }

    protected BlockManager m_BlockManager;
    protected BlockBehaviour m_BlockBehaviour;
    protected BlockActionBehaviour m_BlockActionBehaviour;

    public BlockBehaviour blockBehaviour => m_BlockBehaviour;

    public Transform blockObj => m_BlockManager != null ? m_BlockManager.transform : m_BlockBehaviour?.transform;

    Vector2Int m_vtDuplicate;
    public int horzDuplicate { get => m_vtDuplicate.x; set => m_vtDuplicate.x = value; }
    public int vertDuplicate { get => m_vtDuplicate.y; set => m_vtDuplicate.y = value; }

    int m_nDurability;
    public virtual int durability { get => m_nDurability; set => m_nDurability = value; }

    public bool isMoving => m_BlockManager != null ? m_BlockManager.isMoving : (blockObj != null && m_BlockActionBehaviour != null && m_BlockActionBehaviour.isMoving);

    public Vector2 dropDistance
    {
        set { if (m_BlockManager != null) m_BlockManager.MoveDrop(value); else m_BlockActionBehaviour?.MoveDrop(value); }
    }

    public Block(BlockType blockType)
    {
        m_BlockType = blockType;
        status = BlockStatus.NORMAL;
        questType = BlockQuestType.CLEAR_SIMPLE;
        match = MatchType.NONE;
        m_Breed = BlockBreed.NA;
        m_nDurability = 1;
    }

    // 역할: EMPTY가 아니면 프리팹으로 GameObject 생성. BlockManager 우선, 없으면 BlockBehaviour+BlockActionBehaviour
    internal Block InstantiateBlockObj(GameObject blockPrefab, Transform containerObj)
    {
        if (!IsValidate()) return null;
        var go = Object.Instantiate(blockPrefab, Vector3.zero, Quaternion.identity);
        go.transform.parent = containerObj;
        var manager = go.GetComponent<BlockManager>();
        if (manager != null)
        {
            m_BlockManager = manager;
            m_BlockBehaviour = null;
            m_BlockActionBehaviour = null;
            manager.SetBlock(this);
            return this;
        }
        m_BlockManager = null;
        m_BlockBehaviour = go.GetComponent<BlockBehaviour>();
        m_BlockActionBehaviour = go.GetComponent<BlockActionBehaviour>();
        if (m_BlockBehaviour != null) m_BlockBehaviour.SetBlock(this);
        return this;
    }

    // 역할: 매칭된 블록에 규칙 적용(내구도 감소, CLEAR). Board.Evaluate에서 호출
    public bool DoEvaluation(BoardEnumerator boardEnumerator, int nRow, int nCol)
    {
        Debug.Assert(boardEnumerator != null);
        if (!IsEvaluatable()) return false;

        if (status == BlockStatus.MATCH)
        {
            if (questType == BlockQuestType.CLEAR_SIMPLE || boardEnumerator.IsCageTypeCell(nRow, nCol))
            {
                Debug.Assert(m_nDurability > 0);
                durability--;
            }
            else return true;

            if (m_nDurability == 0) { status = BlockStatus.CLEAR; return false; }
        }

        status = BlockStatus.NORMAL;
        match = MatchType.NONE;
        matchCount = 0;
        return false;
    }

    // 역할: 이 블록을 "매칭됨"으로 표시, 매치 타입/개수 기록 (가로+세로 동시 시 bAccumulate)
    public void UpdateBlockStatusMatched(MatchType matchType, bool bAccumulate = true)
    {
        status = BlockStatus.MATCH;
        match = match == MatchType.NONE ? matchType : (bAccumulate ? match.Add(matchType) : matchType);
        matchCount = (short)matchType;
    }

    internal void Move(float x, float y) { if (blockObj != null) blockObj.position = new Vector3(x, y); }

    // 역할: 목표 위치로 duration 동안 이동 (스와이프 애니메이션)
    public void MoveTo(Vector3 to, float duration)
    {
        var runner = m_BlockManager != null ? (MonoBehaviour)m_BlockManager : m_BlockBehaviour;
        if (runner != null && blockObj != null) runner.StartCoroutine(Action2D.MoveTo(blockObj, to, duration));
    }

    public virtual void Destroy()
    {
        if (m_BlockManager != null) m_BlockManager.DoActionClear();
        else if (m_BlockBehaviour != null) m_BlockBehaviour.DoActionClear();
    }

    public bool IsValidate() => type != BlockType.EMPTY;
    public void ResetDuplicationInfo() { m_vtDuplicate.x = 0; m_vtDuplicate.y = 0; }
    public bool IsEqual(Block target) => IsMatchableBlock() && target != null && breed == target.breed;
    public bool IsMatchableBlock() => type != BlockType.EMPTY;
    public bool IsSwipeable(Block baseBlock) => true;
    public bool IsEvaluatable() => status != BlockStatus.CLEAR && IsMatchableBlock();
}

// -----------------------------------------------------------------------------
// [3] 블록 표시·제거 연출 — 스프라이트, 셀 크기 맞춤, 폭발 후 제거
// -----------------------------------------------------------------------------
public class BlockBehaviour : MonoBehaviour
{
    Block m_Block;
    SpriteRenderer m_SpriteRenderer;
    [SerializeField] BlockConfig m_BlockConfig;

    void Start()
    {
        m_SpriteRenderer = GetComponent<SpriteRenderer>();
        UpdateView(false);
        FitToCellSize(1f);
    }

    public void FitToCellSize(float cellSize)
    {
        if (m_SpriteRenderer == null || m_SpriteRenderer.sprite == null) return;
        var size = m_SpriteRenderer.sprite.bounds.size;
        if (size.x <= 0 || size.y <= 0) return;
        transform.localScale = new Vector3(cellSize / size.x, cellSize / size.y, 1f);
    }

    internal void SetBlock(Block block) => m_Block = block;

    public void UpdateView(bool bValueChanged)
    {
        if (m_Block.type == BlockType.EMPTY) m_SpriteRenderer.sprite = null;
        else if (m_Block.type == BlockType.BASIC) m_SpriteRenderer.sprite = m_BlockConfig.basicBlockSprites[(int)m_Block.breed];
    }

    public void DoActionClear() => StartCoroutine(CoStartSimpleExplosion(true));

    IEnumerator CoStartSimpleExplosion(bool bDestroy = true)
    {
        yield return Action2D.Scale(transform, Constants.BLOCK_DESTROY_SCALE, 4f);
        var explosionObj = m_BlockConfig.GetExplosionObject(BlockQuestType.CLEAR_SIMPLE);
        var main = explosionObj.GetComponent<ParticleSystem>().main;
        main.startColor = m_BlockConfig.GetBlockColor(m_Block.breed);
        explosionObj.SetActive(true);
        explosionObj.transform.position = transform.position;
        yield return new WaitForSeconds(0.1f);
        if (bDestroy) Destroy(gameObject);
        else Debug.Assert(false);
    }
}

// -----------------------------------------------------------------------------
// [4] 블록 드롭(낙하) 연출 — 이동 큐, dropSpeed에 따라 연속 낙하
// -----------------------------------------------------------------------------
public class BlockActionBehaviour : MonoBehaviour
{
    [SerializeField] BlockConfig m_BlockConfig;
    public bool isMoving { get; set; }
    Queue<Vector3> m_MovementQueue = new Queue<Vector3>();

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
            float duration = m_BlockConfig.dropSpeed[dropIndex - 1];
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

// -----------------------------------------------------------------------------
// [5] 블록 생성 — BlockType에 맞는 Block 생성, BASIC이면 breed 랜덤
// -----------------------------------------------------------------------------
public static class BlockFactory
{
    public static Block SpawnBlock(BlockType blockType)
    {
        var block = new Block(blockType);
        if (blockType == BlockType.BASIC) block.breed = (BlockBreed)Random.Range(0, 6);
        else if (blockType == BlockType.EMPTY) block.breed = BlockBreed.NA;
        return block;
    }
}
