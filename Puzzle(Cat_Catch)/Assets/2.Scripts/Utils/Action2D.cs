// ============================================================================
// Action2D.cs - 2D 오브젝트용 단순 애니메이션 코루틴
// ============================================================================
// 설명: Transform을 목표 위치로 이동(MoveTo)하거나 목표 스케일로 축소/확대(Scale)하는 코루틴을 제공합니다.
// 이유: 스와이프·드롭·블록 제거 연출 등 공통으로 쓰이므로 한 곳에 두고 재사용합니다.
// ============================================================================

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Action2D
{
    /// <summary>duration 동안 target을 to 위치로 선형 보간 이동. bSelfRemove면 종료 후 GameObject 제거</summary>
    public static IEnumerator MoveTo(Transform target, Vector3 to, float duration, bool bSelfRemove = false)
    {
        Vector2 startPos = target.transform.position;

        float elapsed = 0.0f;
        while (elapsed < duration)
        {
            elapsed += Time.smoothDeltaTime;
            target.transform.position = Vector2.Lerp(startPos, to, elapsed / duration);

            yield return null;
        }

        target.transform.position = to;

        if (bSelfRemove)
            Object.Destroy(target.gameObject, 0.1f);

        yield break;
    }

    /// <summary>target의 localScale을 toScale까지 speed 속도로 변경. 블록 제거 시 축소 연출에 사용</summary>
    public static IEnumerator Scale(Transform target, float toScale, float speed)
    {
        bool bInc = target.localScale.x < toScale;
        float fDir = bInc ? 1 : -1;

        float factor;
        while (true)
        {
            factor = Time.deltaTime * speed * fDir;
            target.localScale = new Vector3(target.localScale.x + factor, target.localScale.y + factor, target.localScale.z);

            if ((!bInc && target.localScale.x <= toScale) || (bInc && target.localScale.x >= toScale))
                break;

            yield return null;
        }

        yield break;
    }
}
