// ============================================================================
// BoardEnumerator.cs - 보드 순회 및 셀 타입별 판정
// ============================================================================
// 설명: Board를 참조해 특정 칸이 "케이지" 등 특수 셀인지 판정합니다.
// 이유: Block.DoEvaluation에서 "케이지 안 블록은 단순 제거" 등 규칙을 나눌 때 사용하며, 확장 시 여기만 수정하면 됩니다.
// ============================================================================

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Ninez.Board;

namespace Ninez.Board
{
    public class BoardEnumerator
    {
        Ninez.Board.Board m_Board;

        public BoardEnumerator(Ninez.Board.Board board)
        {
            this.m_Board = board;
        }

        /// <summary>해당 칸이 케이지(가두기) 타입 셀인지. true면 블록이 단순 제거 규칙 적용(퀘스트 확장용)</summary>
        public bool IsCageTypeCell(int nRow, int nCol)
        {
            return false;
        }
    }
}