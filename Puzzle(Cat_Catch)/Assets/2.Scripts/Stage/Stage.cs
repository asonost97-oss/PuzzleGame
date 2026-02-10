// =============================================================================
// Stage.cs — 스테이지 데이터 구조, 로드, 빌드, 한 판 로직 (통합)
// =============================================================================

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// -----------------------------------------------------------------------------
// [1] 스테이지 데이터 구조 — JSON(row, col, cells) 역직렬화용, 필드명 공개
// -----------------------------------------------------------------------------
[Serializable]
public class StageInfo
{
    public int row;
    public int col;
    public int[] cells;

    public override string ToString() => JsonUtility.ToJson(this);

    // 역할: (nRow, nCol)에 해당하는 셀 타입 반환. 파일 좌표계 ↔ 보드 좌표계 보정
    public CellType GetCellType(int nRow, int nCol)
    {
        Debug.Assert(cells != null && cells.Length > nRow * col + nCol);
        int revisedRow = (row - 1) - nRow;
        if (cells.Length > revisedRow * col + nCol)
            return (CellType)cells[revisedRow * col + nCol];
        Debug.Assert(false);
        return CellType.EMPTY;
    }

    public bool DoValidation()
    {
        Debug.Assert(cells != null && cells.Length == row * col);
        return cells.Length == row * col;
    }
}

// -----------------------------------------------------------------------------
// [2] 스테이지 로드 — Resources/Stage/stage_XXXX 텍스트 → StageInfo
// -----------------------------------------------------------------------------
public static class StageReader
{
    // 역할: Resources/Stage/stage_0001 형식으로 로드 후 JsonUtility로 StageInfo 반환
    public static StageInfo LoadStage(int nStage)
    {
        var textAsset = Resources.Load<TextAsset>($"Stage/stage_{nStage:D4}");
        if (textAsset != null)
        {
            var info = JsonUtility.FromJson<StageInfo>(textAsset.text);
            Debug.Assert(info.DoValidation());
            return info;
        }
        return null;
    }
}

// -----------------------------------------------------------------------------
// [3] 스테이지 빌드 — 스테이지 번호 → StageInfo 로드 → Stage·Cell·Block 배열 채움
// -----------------------------------------------------------------------------
public class StageBuilder
{
    StageInfo m_StageInfo;
    int m_nStage;

    public StageBuilder(int nStage) => m_nStage = nStage;

    // 역할: 데이터만 채워진 Stage 반환 (ComposeStage는 별도로 GameObject 생성)
    public Stage ComposeStage()
    {
        Debug.Assert(m_nStage > 0);
        m_StageInfo = StageReader.LoadStage(m_nStage);
        if (m_StageInfo == null)
            throw new Exception($"스테이지 {m_nStage} 데이터를 로드할 수 없습니다. Resources/Stage/stage_{m_nStage:D4} 확인하세요.");

        var stage = new Stage(this, m_StageInfo.row, m_StageInfo.col);
        for (int r = 0; r < m_StageInfo.row; r++)
            for (int c = 0; c < m_StageInfo.col; c++)
            {
                stage.blocks[r, c] = SpawnBlockForStage(r, c);
                stage.cells[r, c] = SpawnCellForStage(r, c);
            }
        return stage;
    }

    public StageInfo LoadStage(int nStage) => StageReader.LoadStage(nStage);

    Block SpawnBlockForStage(int r, int c) =>
        m_StageInfo.GetCellType(r, c) == CellType.EMPTY ? SpawnEmptyBlock() : SpawnBlock();

    Cell SpawnCellForStage(int r, int c)
    {
        Debug.Assert(m_StageInfo != null && r < m_StageInfo.row && c < m_StageInfo.col);
        return CellFactory.SpawnCell(m_StageInfo, r, c);
    }

    public static Stage BuildStage(int nStage) => new StageBuilder(nStage).ComposeStage();
    public Block SpawnBlock() => BlockFactory.SpawnBlock(BlockType.BASIC);
    public Block SpawnEmptyBlock() => BlockFactory.SpawnBlock(BlockType.EMPTY);
}

// -----------------------------------------------------------------------------
// [4] 스테이지(한 판) — Board 소유, 스와이프/평가/후처리, 입력용 좌표·스와이프 판정
// -----------------------------------------------------------------------------
public class Stage
{
    public int maxRow => m_Board.maxRow;
    public int maxCol => m_Board.maxCol;
    public Board board => m_Board;
    public Block[,] blocks => m_Board.blocks;
    public Cell[,] cells => m_Board.cells;

    Board m_Board;
    StageBuilder m_StageBuilder;

    public Stage(StageBuilder stageBuilder, int nRow, int nCol)
    {
        m_StageBuilder = stageBuilder;
        m_Board = new Board(nRow, nCol);
    }

    internal void ComposeStage(GameObject cellPrefab, GameObject blockPrefab, Transform container) =>
        m_Board.ComposeStage(cellPrefab, blockPrefab, container, m_StageBuilder);

    // 역할: (row,col) 블록을 swipeDir 방향 인접 블록과 교환(애니 후 배열 갱신). 성공 시 actionResult=true
    public IEnumerator CoDoSwipeAction(int nRow, int nCol, Swipe swipeDir, Returnable<bool> actionResult)
    {
        actionResult.value = false;
        int nSwipeRow = nRow + swipeDir.GetTargetRow(), nSwipeCol = nCol + swipeDir.GetTargetCol();

        Debug.Assert(nRow != nSwipeRow || nCol != nSwipeCol);
        Debug.Assert(nSwipeRow >= 0 && nSwipeRow < maxRow && nSwipeCol >= 0 && nSwipeCol < maxCol);

        if (m_Board.IsSwipeable(nSwipeRow, nSwipeCol))
        {
            var targetBlock = blocks[nSwipeRow, nSwipeCol];
            var baseBlock = blocks[nRow, nCol];
            if (baseBlock == null || targetBlock == null ||
                baseBlock.blockObj == null || targetBlock.blockObj == null)
                yield break;

            var basePos = baseBlock.blockObj.transform.position;
            var targetPos = targetBlock.blockObj.transform.position;

            if (targetBlock.IsSwipeable(baseBlock))
            {
                baseBlock.MoveTo(targetPos, Constants.SWIPE_DURATION);
                targetBlock.MoveTo(basePos, Constants.SWIPE_DURATION);
                yield return new WaitForSeconds(Constants.SWIPE_DURATION);

                blocks[nRow, nCol] = targetBlock;
                blocks[nSwipeRow, nSwipeCol] = baseBlock;
                actionResult.value = true;
            }
        }
    }

    public IEnumerator Evaluate(Returnable<bool> matchResult) => m_Board.Evaluate(matchResult);

    // 역할: 매치 제거 후 드롭 → 빈 칸 스폰 → 드롭 끝날 때까지 대기
    public IEnumerator PostprocessAfterEvaluate()
    {
        var unfilledBlocks = new List<KeyValuePair<int, int>>();
        var movingBlocks = new List<Block>();
        yield return m_Board.ArrangeBlocksAfterClean(unfilledBlocks, movingBlocks);
        yield return m_Board.SpawnBlocksAfterClean(movingBlocks);
        yield return WaitForDropping(movingBlocks);
    }

    public IEnumerator WaitForDropping(List<Block> movingBlocks)
    {
        var wait = new WaitForSeconds(0.05f);
        while (true)
        {
            bool anyMoving = false;
            for (int i = 0; i < movingBlocks.Count; i++)
                if (movingBlocks[i].isMoving) { anyMoving = true; break; }
            if (!anyMoving) break;
            yield return wait;
        }
        movingBlocks.Clear();
    }

    #region Input / Swipe (StageController에서 사용)

    void LocalToGrid(Vector2 local, out int row, out int col)
    {
        col = (int)(local.x + maxCol * 0.5f);
        row = (int)(local.y + maxRow * 0.5f);
    }

    public bool IsInsideBoard(Vector2 local)
    {
        LocalToGrid(local, out int row, out int col);
        return row >= 0 && row < maxRow && col >= 0 && col < maxCol;
    }

    public bool IsOnValideBlock(Vector2 local, out BlockPos blockPos)
    {
        LocalToGrid(local, out int row, out int col);
        blockPos = new BlockPos(row, col);
        return board.IsSwipeable(row, col);
    }

    public bool IsValideSwipe(int row, int col, Swipe dir)
    {
        switch (dir)
        {
            case Swipe.DOWN: return row > 0;
            case Swipe.UP: return row < maxRow - 1;
            case Swipe.LEFT: return col > 0;
            case Swipe.RIGHT: return col < maxCol - 1;
            default: return false;
        }
    }
    #endregion

    public void PrintAll()
    {
        var sbCells = new System.Text.StringBuilder();
        var sbBlocks = new System.Text.StringBuilder();
        for (int r = maxRow - 1; r >= 0; r--)
        {
            for (int c = 0; c < maxCol; c++)
            {
                sbCells.Append($"{cells[r, c].type}, ");
                sbBlocks.Append($"{blocks[r, c]?.breed}, ");
            }
            sbCells.AppendLine(); sbBlocks.AppendLine();
        }
        Debug.Log(sbCells.ToString()); Debug.Log(sbBlocks.ToString());
    }
}
