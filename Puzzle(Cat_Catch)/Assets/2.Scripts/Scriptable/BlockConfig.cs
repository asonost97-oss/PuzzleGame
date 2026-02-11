// ============================================================================
// BlockConfig.cs - 블록 시각·연출 설정 (ScriptableObject)
// ============================================================================
// 설명: 드롭 속도, 기본 블록 스프라이트, 색상을 에디터에서 할당하고 런타임에 참조합니다.
// 파티클 프리팹은 BlockManager(또는 BlockBehaviour)의 particlePrefab에서 할당합니다.
// ============================================================================

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Bingle/Block Config", fileName = "BlockConfig.asset")]
public class BlockConfig : ScriptableObject
{
    public float[] dropSpeed;           // 낙하 칸 수별 재생 시간 (인덱스 0=1칸, ...)
    public Sprite[] basicBlockSprites; // breed별 스프라이트
}