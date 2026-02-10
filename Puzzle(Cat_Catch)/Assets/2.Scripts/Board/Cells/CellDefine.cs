// ============================================================================
// CellDefine.cs - 셀 타입 열거형 및 확장 메서드
// ============================================================================
// 설명: EMPTY/BASIC/FIXTURE/JELLY 등 칸의 종류를 정의하고, "블록 배치 가능/이동 가능" 판정 메서드를 제공합니다.
// 이유: 보드 파일에서 숫자로 저장된 셀 타입을 의미 있게 쓰고, 확장 시 한 곳만 수정하면 됩니다.
// ============================================================================

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Ninez.Board
{
    /// <summary>보드 한 칸의 종류. 블록이 올 수 있는지, 드롭 통과인지 등 결정</summary>
    public enum CellType
    {
        EMPTY = 0,      // 빈공간, 블럭 배치 불가, 드롭은 통과
        BASIC = 1,      // 기본 칸 (블록 배치·이동 가능)
        FIXTURE = 2,    // 고정 장애물
        JELLY = 3,      // 젤리: 블록 이동 가능, 클리어 시 BASIC 등으로 변경 가능 (확장용)
    }

    /// <summary>CellType에 대한 확장 메서드. 블록 배치/이동 가능 여부 판정</summary>
    static class CellTypeMethod
    {
        /// <summary>이 셀 타입에 블록을 새로 배치할 수 있는지 (EMPTY가 아니면 true)</summary>
        public static bool IsBlockAllocatableType(this CellType cellType)
        {
            return !(cellType == CellType.EMPTY);
        }

        /// <summary>이 셀 타입에서 블록이 스와이프 등으로 이동 가능한지</summary>
        public static bool IsBlockMovableType(this CellType cellType)
        {
            return !(cellType == CellType.EMPTY);
        }
    }
}