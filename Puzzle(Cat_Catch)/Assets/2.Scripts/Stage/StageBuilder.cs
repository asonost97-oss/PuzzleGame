// ============================================================================
// StageBuilder.cs - 스테이지 데이터 로드 및 Stage/Board 초기 구성
// ============================================================================
// 설명: 스테이지 번호로 StageInfo를 로드하고, 그에 맞게 Stage·Cell·Block을 생성해 "데이터만 채워진" Stage를 반환합니다.
// 이유: 씬 구성(ComposeStage)과 데이터 구성을 분리해, 테스트나 다른 씬에서 같은 스테이지를 재사용하기 쉽게 합니다.
// ============================================================================

using System;
using UnityEngine;
public class StageBuilder
{
    StageInfo m_StageInfo;
    int m_nStage;

    public StageBuilder(int nStage)
    {
        m_nStage = nStage;
    }

    public Stage ComposeStage()
    {
        Debug.Assert(m_nStage > 0, $"Invalide Stage : {m_nStage}");

        m_StageInfo = LoadStage(m_nStage);
        if (m_StageInfo == null)
        {
            throw new System.Exception($"스테이지 {m_nStage} 데이터를 로드할 수 없습니다. Resources/Stage/stage_{m_nStage:D4} 파일이 있는지 확인하세요.");
        }

        Stage stage = new Stage(this, m_StageInfo.row, m_StageInfo.col);

        for (int nRow = 0; nRow < m_StageInfo.row; nRow++)
        {
            for (int nCol = 0; nCol < m_StageInfo.col; nCol++)
            {
                stage.blocks[nRow, nCol] = SpawnBlockForStage(nRow, nCol);
                stage.cells[nRow, nCol] = SpawnCellForStage(nRow, nCol);
            }
        }

        return stage;
    }

    public StageInfo LoadStage(int nStage)
    {
        return StageReader.LoadStage(nStage);
    }

    Block SpawnBlockForStage(int nRow, int nCol)
    {
        if (m_StageInfo.GetCellType(nRow, nCol) == CellType.EMPTY)
            return SpawnEmptyBlock();

        return SpawnBlock();
    }

    Cell SpawnCellForStage(int nRow, int nCol)
    {
        Debug.Assert(m_StageInfo != null);
        Debug.Assert(nRow < m_StageInfo.row && nCol < m_StageInfo.col);

        return CellFactory.SpawnCell(m_StageInfo, nRow, nCol);
    }

    public static Stage BuildStage(int nStage)
    {
        StageBuilder stageBuilder = new StageBuilder(nStage);
        return stageBuilder.ComposeStage();
    }

    public Block SpawnBlock()
    {
        return BlockFactory.SpawnBlock(BlockType.BASIC);
    }

    public Block SpawnEmptyBlock()
    {
        return BlockFactory.SpawnBlock(BlockType.EMPTY);
    }
}
