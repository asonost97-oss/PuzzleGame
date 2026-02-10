// ============================================================================
// CellFactory.cs - Cell 인스턴스 생성
// ============================================================================
// 설명: StageInfo의 (row,col)에 해당하는 셀 타입을 가져와 Cell을 생성합니다.
// 이유: 스테이지 데이터 → Cell 생성 로직을 한 곳에 모아 StageBuilder가 단순해집니다.
// ============================================================================

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class CellFactory
{
    /// <summary>스테이지 정보에서 (nRow, nCol)의 셀 타입을 읽어 해당 타입의 Cell 생성</summary>
    public static Cell SpawnCell(StageInfo stageInfo, int nRow, int nCol)
    {
        Debug.Assert(stageInfo != null);
        Debug.Assert(nRow < stageInfo.row && nCol < stageInfo.col);
        return SpawnCell(stageInfo.GetCellType(nRow, nCol));
    }

    /// <summary>지정 CellType의 Cell 인스턴스 생성</summary>
    public static Cell SpawnCell(CellType cellType)
    {
        return new Cell(cellType);
    }
}
