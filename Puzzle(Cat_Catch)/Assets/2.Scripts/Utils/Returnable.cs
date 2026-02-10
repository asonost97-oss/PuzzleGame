// ============================================================================
// Returnable.cs - 코루틴에서 결과를 넘겨받기 위한 래퍼 클래스
// ============================================================================
// 설명: IEnumerator는 반환 타입으로 값을 줄 수 없으므로, 참조로 전달할 수 있는 Returnable<T>에 결과를 넣어 호출자가 읽습니다.
// 이유: 예) Returnable<bool> matchResult; yield return Stage.Evaluate(matchResult); 후 matchResult.value로 매치 여부 확인.
// ============================================================================

public class Returnable<T>
{
    public T value { get; set; }

    public Returnable(T value)
    {
        this.value = value;
    }
}
