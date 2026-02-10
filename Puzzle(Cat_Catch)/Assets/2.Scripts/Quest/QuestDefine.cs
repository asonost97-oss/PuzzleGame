// ============================================================================
// QuestDefine.cs - 매칭 형태(MatchType) 및 조합 확장 메서드
// ============================================================================
// 설명: 3/4/5매치와 T+L형(3+3, 3+4 등)을 숫자로 정의하고, 가로+세로 동시 매치 시 조합(Add)해 퀘스트/특수 블록 타입을 결정합니다.
// 이유: BlockQuestType과 연결해 "이 매칭이면 이 효과"를 한 곳에서 관리하고, 확장 시 열거형만 추가하면 됩니다.
// ============================================================================

using System;

public enum MatchType
{
    NONE = 0,
    THREE = 3,       // 3매치 → CLEAR_SIMPLE
    FOUR = 4,        // 4매치 → CLEAR_HORZ 또는 VERT
    FIVE = 5,        // 5매치 → CLEAR_LAZER
    THREE_THREE = 6,  // 3+3 (T/L) → CLEAR_CIRCLE
    THREE_FOUR = 7,
    THREE_FIVE = 8,
    FOUR_FIVE = 9,
    FOUR_FOUR = 10,
}

static class MatchTypeMethod
{
    public static short ToValue(this MatchType matchType)
    {
        return (short)matchType;
    }

    /// <summary>가로 매치 + 세로 매치 등 두 매치를 합침. FOUR+FOUR → FOUR_FOUR 등</summary>
    public static MatchType Add(this MatchType matchTypeSrc, MatchType matchTypeTarget)
    {
        if (matchTypeSrc == MatchType.FOUR && matchTypeTarget == MatchType.FOUR)
            return MatchType.FOUR_FOUR;

        return (MatchType)((int)matchTypeSrc + (int)matchTypeTarget);
    }
}