// ============================================================================
// ParticleAutoDestroy.cs - 파티클 재생 종료 시 자동으로 GameObject 제거
// ============================================================================
// 설명: ParticleSystem이 붙은 GameObject를 켜두고, 파티클이 완전히 재생 끝나면(IsAlive false) 자동으로 Destroy합니다.
// 이유: 블록 폭발 등 일회성 이펙트를 Instantiate만 하고 수명 관리하지 않아도 되어, 호출 측 코드가 단순해집니다.
// ============================================================================

using UnityEngine;
using System.Collections;

[RequireComponent(typeof(ParticleSystem))]
public class ParticleAutoDestroy : MonoBehaviour
{
    void OnEnable()
    {
        StartCoroutine(CoCheckAlive());
    }

    IEnumerator CoCheckAlive()
    {
        while (true)
        {
            yield return new WaitForSeconds(0.5f);
            if (!GetComponent<ParticleSystem>().IsAlive(true))
            {
                Destroy(this.gameObject);
                break;
            }
        }
    }
}
