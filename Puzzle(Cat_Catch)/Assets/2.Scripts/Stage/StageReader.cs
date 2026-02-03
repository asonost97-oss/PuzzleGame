using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class StageReader
{
    //public static StageInfo LoadStage(int nStage)   // ① 반환타입 + 메서드이름
    //{
    //    TextAsset textAsset = Resources.Load<TextAsset>($"Stage/{GetFileName(nStage)}");  // ② TextAsset

    //    if (textAsset != null)
    //    {
    //        StageInfo stageInfo = JsonUtility.FromJson<StageInfo>(textAsset.text);

    //        Debug.Assert(stageInfo.DoValidation());

    //        return stageInfo;
    //    }

    //    return null;
    //}

    //static string GetFileName(int nStage)
    //{
    //    return string.Format("stage_{0:D4}", nStage);
    //}
}