// ============================================================================
// useful.cs - SortedList 등 유틸리티 확장 메서드
// ============================================================================
// 설명: SortedList의 "첫 번째 요소"를 KeyValuePair로 반환하는 확장 메서드. BoardShuffler에서 빈 칸 처리 시 사용합니다.
// 이유: .NET SortedList에는 First()가 없어, Keys[0]/Values[0]으로 접근하는 코드를 한 곳에 모아 가독성을 높입니다.
// ============================================================================

using System.Collections.Generic;

public static class SortedListMethods
{
    /// <summary>SortedList의 첫 번째 키-값 쌍 반환. 비어 있으면 default. 호출 전 Count 확인 권장</summary>
    public static KeyValuePair<T1, T2> First<T1, T2>(this SortedList<T1, T2> sortedList)
    {
        if (sortedList.Count == 0)
            return new KeyValuePair<T1, T2>();
        return new KeyValuePair<T1, T2>(sortedList.Keys[0], sortedList.Values[0]);
    }
}