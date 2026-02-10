// ============================================================================
// StageReader.cs - Resources에서 스테이지 JSON 로드
// ============================================================================
// 설명: Resources/Stage/stage_0001 형식의 텍스트 에셋을 읽어 StageInfo로 역직렬화합니다.
// 이유: 스테이지 데이터를 코드 밖에서 수정할 수 있게 하고, 빌드에 리소스로 포함되면 경로만 맞추면 됩니다.
// ============================================================================

using UnityEngine;

public static class StageReader
{
    /// <summary>Resources/Stage/stage_XXXX 에셋 로드 후 JsonUtility로 StageInfo 반환. 없으면 null</summary>
    public static StageInfo LoadStage(int nStage)
    {
        Debug.Log($"Load Stage : Stage/{GetFileName(nStage)}");

        TextAsset textAsset = Resources.Load<TextAsset>($"Stage/{GetFileName(nStage)}");
        if (textAsset != null)
        {
            StageInfo stageInfo = JsonUtility.FromJson<StageInfo>(textAsset.text);
            Debug.Assert(stageInfo.DoValidation());
            return stageInfo;
        }

        return null;
    }

    /// <summary>스테이지 번호를 4자리 파일명으로 변환. 예: 1 → "stage_0001" (확장자 없음, Resources 규칙)</summary>
    static string GetFileName(int nStage)
    {
        return string.Format("stage_{0:D4}", nStage);
    }
}
