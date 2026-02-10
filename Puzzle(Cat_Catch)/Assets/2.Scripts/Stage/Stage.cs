// ============================================================================
// Stage.cs - 한 스테이지(한 판)의 진입점: 보드 참조, 스와이프/평가/후처리
// ============================================================================
// 설명: Board를 소유하고, 스와이프 액션·보드 평가·매칭 후 드롭/스폰 후처리를 코루틴으로 제공합니다.
// 이유: StageController는 입력만 받고, 실제 규칙·연출은 Stage에 모아 "한 판" 단위 테스트와 재사용이 쉽게 합니다.
// ============================================================================

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class Stage
{
    public int maxRow { get { return m_Board.maxRow; } }
    public int maxCol { get { return m_Board.maxCol; } }

    Board m_Board;
    public Board board { get { return m_Board; } }

    StageBuilder m_StageBuilder;

    public Block[,] blocks { get { return m_Board.blocks; } }
    public Cell[,] cells { get { return m_Board.cells; } }

    /// <summary>StageBuilder가 보드 크기와 구성을 알려주고, Board 인스턴스를 생성합니다.</summary>
    public Stage(StageBuilder stageBuilder, int nRow, int nCol)
    {
        m_StageBuilder = stageBuilder;
        m_Board = new Board(nRow, nCol);
    }

    /// <summary>Cell/Block 프리팹과 컨테이너로 보드를 시각적으로 구성(셔플 후 GameObject 생성·배치)</summary>
    internal void ComposeStage(GameObject cellPrefab, GameObject blockPrefab, Transform container)
    {
        m_Board.ComposeStage(cellPrefab, blockPrefab, container, m_StageBuilder);
    }

    /// <summary>
    /// (nRow, nCol) 블록을 swipeDir 방향으로 인접 블록과 교환합니다.
    /// 성공 시 actionResult=true, 실패(스와이프 불가 칸 등)면 false. 교환 후 평가는 ActionManager에서 처리.
    /// </summary>
    public IEnumerator CoDoSwipeAction(int nRow, int nCol, Swipe swipeDir, Returnable<bool> actionResult)
    {
        actionResult.value = false;

        int nSwipeRow = nRow, nSwipeCol = nCol;
        nSwipeRow += swipeDir.GetTargetRow();
        nSwipeCol += swipeDir.GetTargetCol();

        Debug.Assert(nRow != nSwipeRow || nCol != nSwipeCol, "Invalid Swipe : ({nSwipeRow}, {nSwipeCol})");
        Debug.Assert(nSwipeRow >= 0 && nSwipeRow < maxRow && nSwipeCol >= 0 && nSwipeCol < maxCol, $"Swipe 타겟 블럭 인덱스 오류 = ({nSwipeRow}, {nSwipeCol}) ");

        if (m_Board.IsSwipeable(nSwipeRow, nSwipeCol))
        {
            Block targetBlock = blocks[nSwipeRow, nSwipeCol];
            Block baseBlock = blocks[nRow, nCol];
            Debug.Assert(baseBlock != null && targetBlock != null);

            Vector3 basePos = baseBlock.blockObj.transform.position;
            Vector3 targetPos = targetBlock.blockObj.transform.position;

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

        yield break;
    }

    /// <summary>보드 전체 3매치 검사 후 매치된 블록 제거. matchResult에 매치 존재 여부 전달</summary>
    public IEnumerator Evaluate(Returnable<bool> matchResult)
    {
        yield return m_Board.Evaluate(matchResult);
    }

    /// <summary>
    /// 매칭 제거 후: 빈 칸으로 기존 블록 드롭 → 남은 빈 칸에 새 블록 스폰 → 드롭 애니메이션 종료까지 대기.
    /// </summary>
    public IEnumerator PostprocessAfterEvaluate()
    {
        List<KeyValuePair<int, int>> unfilledBlocks = new List<KeyValuePair<int, int>>();
        List<Block> movingBlocks = new List<Block>();

        yield return m_Board.ArrangeBlocksAfterClean(unfilledBlocks, movingBlocks);
        yield return m_Board.SpawnBlocksAfterClean(movingBlocks);
        yield return WaitForDropping(movingBlocks);
    }

    /// <summary>드롭 중인 블록이 모두 isMoving=false가 될 때까지 짧은 간격으로 폴링하여 대기</summary>
    public IEnumerator WaitForDropping(List<Block> movingBlocks)
    {
        WaitForSeconds waitForSecond = new WaitForSeconds(0.05f);

        while (true)
        {
            bool bContinue = false;
            for (int i = 0; i < movingBlocks.Count; i++)
            {
                if (movingBlocks[i].isMoving)
                {
                    bContinue = true;
                    break;
                }
            }

            if (!bContinue)
                break;

            yield return waitForSecond;
        }

        movingBlocks.Clear();
        yield break;
    }

    #region Simple Methods

    /// <summary>월드 좌표 ptOrg가 보드 영역(0~maxCol, 0~maxRow) 안인지. 터치 유효 영역 판단에 사용</summary>
    public bool IsInsideBoard(Vector2 ptOrg)
    {
        Vector2 point = new Vector2(ptOrg.x + (maxCol / 2.0f), ptOrg.y + (maxRow / 2.0f));

        if (point.y < 0 || point.x < 0 || point.y > maxRow || point.x > maxCol)
            return false;

        return true;
    }

    /// <summary>point(컨테이너 기준 월드 좌표)가 스와이프 가능한 블록 위인지 판정하고, 해당 칸 인덱스를 blockPos에 반환</summary>
    public bool IsOnValideBlock(Vector2 point, out BlockPos blockPos)
    {
        Vector2 pos = new Vector2(point.x + (maxCol/ 2.0f), point.y + (maxRow / 2.0f));
        int nRow = (int)pos.y;
        int nCol = (int)pos.x;

        blockPos = new BlockPos(nRow, nCol);
        return board.IsSwipeable(nRow, nCol);
    }

    /// <summary>해당 칸에서 swipeDir 방향으로 스와이프 시 보드 밖으로 나가지 않는지 검사</summary>
    public bool IsValideSwipe(int nRow, int nCol, Swipe swipeDir)
    {
        switch (swipeDir)
        {
            case Swipe.DOWN: return nRow > 0; ;
            case Swipe.UP: return nRow < maxRow - 1;
            case Swipe.LEFT: return nCol > 0;
            case Swipe.RIGHT: return nCol < maxCol - 1;
            default:
                return false;
        }
    }
    #endregion

    public void PrintAll()
    {
        System.Text.StringBuilder strCells = new System.Text.StringBuilder();
        System.Text.StringBuilder strBlocks = new System.Text.StringBuilder();

        for (int nRow = maxRow -1; nRow >=0; nRow--)
        {
            for (int nCol = 0; nCol < maxCol; nCol++)
            {
                strCells.Append($"{cells[nRow, nCol].type}, ");
                strBlocks.Append($"{blocks[nRow, nCol].breed}, ");
            }

            strCells.Append("\n");
            strBlocks.Append("\n");
        }

        Debug.Log(strCells.ToString());
        Debug.Log(strBlocks.ToString());
    }
}