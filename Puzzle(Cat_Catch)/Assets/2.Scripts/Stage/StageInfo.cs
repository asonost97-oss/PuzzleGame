// ============================================================================
// StageInfo.cs - 스테이지 파일(JSON) 데이터 구조
// ============================================================================
// 설명: stage_0001.txt 등에서 로드한 row, col, cells 배열을 담는 클래스. JsonUtility로 직렬화하므로 멤버 이름을 m_ 없이 공개로 둡니다.
// 이유: JSON 키와 필드 이름이 일치해야 로드가 되므로, 프로퍼티가 아닌 public 필드를 사용합니다.
// ============================================================================

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[System.Serializable]
public class StageInfo
{
    public int row;
    public int col;
    public int[] cells;

    public override string ToString()
    {
        return JsonUtility.ToJson(this);
    }

    /// <summary>논리 좌표 (nRow, nCol)에 해당하는 셀 타입 반환. 파일 좌표계와 보드 좌표계 보정 포함</summary>
    public CellType GetCellType(int nRow, int nCol)
    {
        Debug.Assert(cells != null && cells.Length > nRow * col + nCol, $"Invalid Row/Col = {nRow}, {nCol}");

        int revisedRow = (row - 1) - nRow;
        if (cells.Length > revisedRow * col + nCol)
            return (CellType)cells[revisedRow * col + nCol];

        Debug.Assert(false);

        return CellType.EMPTY;
    }

    /// <summary>cells 길이가 row*col과 일치하는지 검사 (디버그/로드 검증용)</summary>
    public bool DoValidation()
    {
        Debug.Assert(cells.Length == row * col);

        if (cells.Length != row * col)
            return false;

        return true;
    }
}
