// ============================================================================
// BlockPos.cs - 보드 위 블록 인덱스 (row, col) 구조체
// ============================================================================
// 설명: 보드 그리드의 (행, 열)을 담는 경량 구조체. 터치 시 "어느 블록을 눌렀는지" 전달할 때 사용합니다.
// 이유: row/col을 묶어서 전달하면 인자 개수가 줄고, Equals/GetHashCode로 비교·딕셔너리 키로 쓸 수 있습니다.
// ============================================================================

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct BlockPos
{
    public int row { get; set; }
    public int col { get; set; }

    public BlockPos(int nRow = 0, int nCol = 0)
    {
        row = nRow;
        col = nCol;
    }

    public override bool Equals(object obj)
    {
        return obj is BlockPos pos && row == pos.row && col == pos.col;
    }

    public override int GetHashCode()
    {
        var hashCode = -928284752;
        hashCode = hashCode * -1521134295 + row.GetHashCode();
        hashCode = hashCode * -1521134295 + col.GetHashCode();
        return hashCode;
    }

    public override string ToString()
    {
        return $"(row = {row}, col = {col})";
    }
}