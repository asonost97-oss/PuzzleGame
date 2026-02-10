// =============================================================================
// Board.cs — 보드 격자, 매칭/제거, 셔플, 드롭/스폰 (통합)
// =============================================================================

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using IntIntKV = System.Collections.Generic.KeyValuePair<int, int>;
using BlockVectorKV = System.Collections.Generic.KeyValuePair<Block, UnityEngine.Vector2Int>;

// -----------------------------------------------------------------------------
// [1] 보드 순회·특수 셀 판정 — 케이지 등 (확장 시 여기만 수정)
// -----------------------------------------------------------------------------
public class BoardEnumerator
{
    Board m_Board;
    public BoardEnumerator(Board board) => m_Board = board;

    // 역할: 해당 칸이 케이지 타입이면 true. Block.DoEvaluation에서 단순 제거 규칙 적용 시 사용
    public bool IsCageTypeCell(int nRow, int nCol) => false;
}

// -----------------------------------------------------------------------------
// [2] 보드 셔플 — 시작 시 3매치 없도록 블록 재배치 (중복 정보로 배치 가능한 블록만 사용)
// -----------------------------------------------------------------------------
public class BoardShuffler
{
    Board m_Board;
    bool m_bLoadingMode;

    SortedList<int, BlockVectorKV> m_OrgBlocks = new SortedList<int, BlockVectorKV>();
    IEnumerator<KeyValuePair<int, BlockVectorKV>> m_it;
    Queue<BlockVectorKV> m_UnusedBlocks = new Queue<BlockVectorKV>();
    bool m_bListComplete;

    public BoardShuffler(Board board, bool bLoadingMode) { m_Board = board; m_bLoadingMode = bLoadingMode; }

    // 역할: 셔플(3매치 없음) → ComposeStage에서 한 번 호출
    public void Shuffle(bool bAnimation = false)
    {
        PrepareDuplicationDatas();
        PrepareShuffleBlocks();
        RunShuffle(bAnimation);
    }

    BlockVectorKV NextBlock(bool bUseQueue)
    {
        if (bUseQueue && m_UnusedBlocks.Count > 0) return m_UnusedBlocks.Dequeue();
        if (!m_bListComplete && m_it.MoveNext()) return m_it.Current.Value;
        m_bListComplete = true;
        return new BlockVectorKV(null, Vector2Int.zero);
    }

    void PrepareDuplicationDatas()
    {
        for (int r = 0; r < m_Board.maxRow; r++)
            for (int c = 0; c < m_Board.maxCol; c++)
            {
                var block = m_Board.blocks[r, c];
                if (block == null) continue;

                if (m_Board.CanShuffle(r, c, m_bLoadingMode))
                    block.ResetDuplicationInfo();
                else
                {
                    block.horzDuplicate = block.vertDuplicate = 1;
                    if (c > 0 && !m_Board.CanShuffle(r, c - 1, m_bLoadingMode) && m_Board.blocks[r, c - 1].IsSafeEqual(block))
                    { block.horzDuplicate = 2; m_Board.blocks[r, c - 1].horzDuplicate = 2; }
                    if (r > 0 && !m_Board.CanShuffle(r - 1, c, m_bLoadingMode) && m_Board.blocks[r - 1, c].IsSafeEqual(block))
                    { block.vertDuplicate = 2; m_Board.blocks[r - 1, c].vertDuplicate = 2; }
                }
            }
    }

    void PrepareShuffleBlocks()
    {
        for (int r = 0; r < m_Board.maxRow; r++)
            for (int c = 0; c < m_Board.maxCol; c++)
            {
                if (!m_Board.CanShuffle(r, c, m_bLoadingMode)) continue;
                while (true)
                {
                    int key = UnityEngine.Random.Range(0, 10000);
                    if (!m_OrgBlocks.ContainsKey(key)) { m_OrgBlocks.Add(key, new BlockVectorKV(m_Board.blocks[r, c], new Vector2Int(c, r))); break; }
                }
            }
        m_it = m_OrgBlocks.GetEnumerator();
    }

    void RunShuffle(bool bAnimation)
    {
        for (int r = 0; r < m_Board.maxRow; r++)
            for (int c = 0; c < m_Board.maxCol; c++)
            {
                if (!m_Board.CanShuffle(r, c, m_bLoadingMode)) continue;
                m_Board.blocks[r, c] = GetShuffledBlock(r, c);
            }
    }

    // 역할: 이 위치에 놓아도 3매치 안 나는 블록을 찾아 배치. 안 되면 큐에 넣고 다음 후보
    Block GetShuffledBlock(int nRow, int nCol)
    {
        BlockBreed prevBreed = BlockBreed.NA;
        Block firstBlock = null;
        bool bUseQueue = true;

        while (true)
        {
            var blockInfo = NextBlock(bUseQueue);
            var block = blockInfo.Key;
            if (block == null) { blockInfo = NextBlock(true); block = blockInfo.Key; }
            // 큐/리스트가 비었을 때(극히 드문 경우) 새 블록 생성
            if (block == null)
                block = BlockFactory.SpawnBlock(BlockType.BASIC);

            if (prevBreed == BlockBreed.NA) prevBreed = block.breed;

            if (m_bListComplete)
            {
                if (firstBlock == null) firstBlock = block;
                else if (ReferenceEquals(firstBlock, block)) m_Board.ChangeBlock(block, prevBreed);
            }

            var vtDup = CalcDuplications(nRow, nCol, block);
            if (vtDup.x > 2 || vtDup.y > 2)
            {
                if (blockInfo.Key != null) m_UnusedBlocks.Enqueue(blockInfo);
                bUseQueue = m_bListComplete || !bUseQueue;
                continue;
            }

            block.vertDuplicate = vtDup.y;
            block.horzDuplicate = vtDup.x;
            if (block.blockObj != null)
                block.Move(m_Board.CalcInitX(Constants.BLOCK_ORG) + nCol, m_Board.CalcInitY(Constants.BLOCK_ORG) + nRow);
            return block;
        }
    }

    Vector2Int CalcDuplications(int nRow, int nCol, Block block)
    {
        int colDup = 1, rowDup = 1;
        if (nCol > 0 && m_Board.blocks[nRow, nCol - 1].IsSafeEqual(block)) colDup += m_Board.blocks[nRow, nCol - 1].horzDuplicate;
        if (nRow > 0 && m_Board.blocks[nRow - 1, nCol].IsSafeEqual(block)) rowDup += m_Board.blocks[nRow - 1, nCol].vertDuplicate;
        if (nCol < m_Board.maxCol - 1 && m_Board.blocks[nRow, nCol + 1].IsSafeEqual(block))
        {
            var right = m_Board.blocks[nRow, nCol + 1];
            colDup += right.horzDuplicate;
            if (right.horzDuplicate == 1) right.horzDuplicate = 2;
        }
        if (nRow < m_Board.maxRow - 1 && m_Board.blocks[nRow + 1, nCol].IsSafeEqual(block))
        {
            var upper = m_Board.blocks[nRow + 1, nCol];
            rowDup += upper.vertDuplicate;
            if (upper.vertDuplicate == 1) upper.vertDuplicate = 2;
        }
        return new Vector2Int(colDup, rowDup);
    }
}

// -----------------------------------------------------------------------------
// [3] 보드 — Cell/Block 배열, ComposeStage, Evaluate, 드롭, 스폰
// -----------------------------------------------------------------------------
public class Board
{
    int m_nRow, m_nCol;
    public int maxRow => m_nRow;
    public int maxCol => m_nCol;

    Cell[,] m_Cells;
    public Cell[,] cells => m_Cells;

    Block[,] m_Blocks;
    public Block[,] blocks => m_Blocks;

    Transform m_Container;
    GameObject m_CellPrefab, m_BlockPrefab;
    StageBuilder m_StageBuilder;
    BoardEnumerator m_Enumerator;

    public Board(int nRow, int nCol)
    {
        m_nRow = nRow; m_nCol = nCol;
        m_Cells = new Cell[nRow, nCol];
        m_Blocks = new Block[nRow, nCol];
        m_Enumerator = new BoardEnumerator(this);
    }

    // 역할: 프리팹/컨테이너 저장 → 셔플(3매치 없음) → Cell/Block GameObject 생성·배치
    internal void ComposeStage(GameObject cellPrefab, GameObject blockPrefab, Transform container, StageBuilder stageBuilder)
    {
        m_CellPrefab = cellPrefab; m_BlockPrefab = blockPrefab; m_Container = container; m_StageBuilder = stageBuilder;
        new BoardShuffler(this, true).Shuffle();

        float initX = CalcInitX(0.5f), initY = CalcInitY(0.5f);
        for (int r = 0; r < m_nRow; r++)
            for (int c = 0; c < m_nCol; c++)
            {
                m_Cells[r, c]?.InstantiateCellObj(cellPrefab, container);
                m_Cells[r, c]?.Move(initX + c, initY + r);
                m_Blocks[r, c]?.InstantiateBlockObj(blockPrefab, container);
                m_Blocks[r, c]?.Move(initX + c, initY + r);
            }
    }

    // 역할: 3매치 찾기 → 매치 블록 MATCH 표시 → DoEvaluation → CLEAR 제거. 매치 없으면 matchResult=false
    public IEnumerator Evaluate(Returnable<bool> matchResult)
    {
        if (!UpdateAllBlocksMatchedStatus()) { matchResult.value = false; yield break; }

        for (int r = 0; r < m_nRow; r++)
            for (int c = 0; c < m_nCol; c++)
                m_Blocks[r, c]?.DoEvaluation(m_Enumerator, r, c);

        var clearBlocks = new List<Block>();
        for (int r = 0; r < m_nRow; r++)
            for (int c = 0; c < m_nCol; c++)
            {
                var b = m_Blocks[r, c];
                if (b != null && b.status == BlockStatus.CLEAR) { clearBlocks.Add(b); m_Blocks[r, c] = null; }
            }
        clearBlocks.ForEach(block => block.Destroy());
        yield return new WaitForSeconds(0.2f);
        matchResult.value = true;
    }

    // 역할: 보드 전체 순회하며 가로/세로 3매치 검사, 매치된 블록에 MATCH 상태 부여
    public bool UpdateAllBlocksMatchedStatus()
    {
        var list = new List<Block>();
        int count = 0;
        for (int r = 0; r < m_nRow; r++)
            for (int c = 0; c < m_nCol; c++)
                if (EvalBlocksIfMatched(r, c, list)) count++;
        return count > 0;
    }

    // 역할: (r,c) 포함 가로/세로 3매치 있으면 해당 블록들에 MATCH 상태 + 매치 개수 기록
    public bool EvalBlocksIfMatched(int nRow, int nCol, List<Block> matchedBlockList)
    {
        bool bFound = false;
        var baseBlock = m_Blocks[nRow, nCol];
        if (baseBlock == null || baseBlock.match != MatchType.NONE || !baseBlock.IsValidate() || m_Cells[nRow, nCol].IsObstracle())
            return false;

        matchedBlockList.Add(baseBlock);
        for (int i = nCol + 1; i < m_nCol; i++) { var b = m_Blocks[nRow, i]; if (!b.IsSafeEqual(baseBlock)) break; matchedBlockList.Add(b); }
        for (int i = nCol - 1; i >= 0; i--) { var b = m_Blocks[nRow, i]; if (!b.IsSafeEqual(baseBlock)) break; matchedBlockList.Insert(0, b); }
        if (matchedBlockList.Count >= 3) { matchedBlockList.ForEach(block => block.UpdateBlockStatusMatched((MatchType)matchedBlockList.Count)); bFound = true; }

        matchedBlockList.Clear();
        matchedBlockList.Add(baseBlock);
        for (int i = nRow + 1; i < m_nRow; i++) { var b = m_Blocks[i, nCol]; if (!b.IsSafeEqual(baseBlock)) break; matchedBlockList.Add(b); }
        for (int i = nRow - 1; i >= 0; i--) { var b = m_Blocks[i, nCol]; if (!b.IsSafeEqual(baseBlock)) break; matchedBlockList.Insert(0, b); }
        if (matchedBlockList.Count >= 3) { matchedBlockList.ForEach(block => block.UpdateBlockStatusMatched((MatchType)matchedBlockList.Count)); bFound = true; }

        matchedBlockList.Clear();
        return bFound;
    }

    // 역할: 매치 제거 후 빈 칸을 위쪽 블록이 떨어져 채우도록 배열 갱신 + 드롭 애니메이션 요청
    public IEnumerator ArrangeBlocksAfterClean(List<IntIntKV> unfilledBlocks, List<Block> movingBlocks)
    {
        var emptyBlocks = new SortedList<int, int>();
        for (int nCol = 0; nCol < m_nCol; nCol++)
        {
            emptyBlocks.Clear();
            for (int nRow = 0; nRow < m_nRow; nRow++)
                if (CanBlockBeAllocatable(nRow, nCol)) emptyBlocks.Add(nRow, nRow);
            if (emptyBlocks.Count == 0) continue;

            var first = emptyBlocks.First();
            for (int nRow = first.Value + 1; nRow < m_nRow; nRow++)
            {
                var block = m_Blocks[nRow, nCol];
                if (block == null || m_Cells[nRow, nCol].type == CellType.EMPTY) continue;

                block.dropDistance = new Vector2(0, nRow - first.Value);
                movingBlocks.Add(block);
                Debug.Assert(!m_Cells[first.Value, nCol].IsObstracle());
                m_Blocks[first.Value, nCol] = block;
                m_Blocks[nRow, nCol] = null;
                emptyBlocks.RemoveAt(0);
                emptyBlocks.Add(nRow, nRow);
                first = emptyBlocks.First();
                nRow = first.Value;
            }
        }
        yield return null;
    }

    // 역할: 남은 빈 칸(맨 위 등)에 새 블록 생성 후 위에서 드롭
    public IEnumerator SpawnBlocksAfterClean(List<Block> movingBlocks)
    {
        for (int nCol = 0; nCol < m_nCol; nCol++)
        {
            for (int nRow = 0; nRow < m_nRow; nRow++)
            {
                if (m_Blocks[nRow, nCol] != null) continue;
                int nSpawnBaseY = 0;
                for (int y = nRow; y < m_nRow; y++)
                {
                    if (m_Blocks[y, nCol] != null || !CanBlockBeAllocatable(y, nCol)) continue;
                    var block = SpawnBlockWithDrop(y, nCol, nSpawnBaseY, nCol);
                    if (block != null) movingBlocks.Add(block);
                    nSpawnBaseY++;
                }
                break;
            }
        }
        yield return null;
    }

    Block SpawnBlockWithDrop(int nRow, int nCol, int nSpawnedRow, int nSpawnedCol)
    {
        float x = CalcInitX(Constants.BLOCK_ORG), y = CalcInitY(Constants.BLOCK_ORG) + m_nRow;
        var block = m_StageBuilder.SpawnBlock().InstantiateBlockObj(m_BlockPrefab, m_Container);
        if (block != null)
        {
            m_Blocks[nRow, nCol] = block;
            block.Move(x + nSpawnedCol, y + nSpawnedRow);
            block.dropDistance = new Vector2(nSpawnedCol - nCol, m_nRow + (nSpawnedRow - nRow));
        }
        return block;
    }

    public float CalcInitX(float offset = 0) => -m_nCol / 2.0f + offset;
    public float CalcInitY(float offset = 0) => -m_nRow / 2.0f + offset;

    public bool CanShuffle(int nRow, int nCol, bool bLoading) => m_Cells[nRow, nCol].type.IsBlockMovableType();

    // 역할: 셔플 시 3매치 안 나도록 breed 변경 (현재 위치에 안 맞는 breed 제외 랜덤)
    public void ChangeBlock(Block block, BlockBreed notAllowedBreed)
    {
        BlockBreed gen;
        do gen = (BlockBreed)UnityEngine.Random.Range(0, 6); while (gen == notAllowedBreed);
        block.breed = gen;
    }

    public bool IsSwipeable(int nRow, int nCol) => m_Cells[nRow, nCol].type.IsBlockMovableType();

    bool CanBlockBeAllocatable(int nRow, int nCol) =>
        m_Cells[nRow, nCol].type.IsBlockAllocatableType() && m_Blocks[nRow, nCol] == null;
}
