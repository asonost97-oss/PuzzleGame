// ============================================================================
// BlockDefine.cs - 블록 관련 열거형 및 확장 메서드
// ============================================================================
// 설명: BlockType, BlockBreed, BlockStatus, BlockQuestType 등 블록의 종류·상태를 정의합니다.
// 이유: 매직 넘버 대신 이름으로 코드 가독성을 높이고, 퀘스트/특수 블록 확장 시 한 곳만 수정하면 됩니다.
// ============================================================================

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Ninez.Board
{
    /// <summary>블록의 논리적 타입. EMPTY는 빈 칸(오브젝트 미생성), BASIC은 일반 매칭 블록</summary>
    public enum BlockType
    {
        EMPTY = 0,
        BASIC = 1
    }

    /// <summary>화면에 보이는 블록 종류(스프라이트/색). 랜덤 또는 스테이지에서 결정</summary>
    public enum BlockBreed
    {
        NA      = -1,   // Not Assigned (EMPTY 등)
        BREED_0 = 0,
        BREED_1 = 1,
        BREED_2 = 2,
        BREED_3 = 3,
        BREED_4 = 4,
        BREED_5 = 5,
    }

    /// <summary>블록의 게임 상태. NORMAL→매칭 검사→MATCH→평가 후 CLEAR로 제거</summary>
    public enum BlockStatus
    {
        NORMAL,                 // 평상시
        MATCH,                  // 3매치 등으로 "제거 대상" 표시됨
        CLEAR                   // 평가 완료, 곧 Destroy() 호출
    }

    /// <summary>블록이 제거될 때 발동하는 효과 타입. 4/5매치·T/L형 등 확장용</summary>
    public enum BlockQuestType  //블럭 클리어 발동 효과
    {
        NONE = -1,
        CLEAR_SIMPLE = 0,       // 단일 블럭 제거 (3매치 기본)
        CLEAR_HORZ = 1,         // 4매치 가로 한 줄
        CLEAR_VERT = 2,         // 4매치 세로 한 줄
        CLEAR_CIRCLE = 3,       // T/L 매치 시 인접 영역
        CLEAR_LAZER = 4,        // 5매치 시 같은 breed 전부
        CLEAR_HORZ_BUFF = 5,    // HORZ + CIRCLE 조합
        CLEAR_VERT_BUFF = 6,    // VERT + CIRCLE 조합
        CLEAR_CIRCLE_BUFF = 7,  // CIRCLE + CIRCLE 조합
        CLEAR_LAZER_BUFF = 8    // LAZER + LAZER 조합
    }

    /// <summary>Block에 대한 확장 메서드. null 체크 후 IsEqual 호출해 NullReference 방지</summary>
    static class BlockMethod
    {
        public static bool IsSafeEqual(this Block block, Block targetBlock)
        {
            if (block == null)
                return false;
            return block.IsEqual(targetBlock);
        }
    }
}