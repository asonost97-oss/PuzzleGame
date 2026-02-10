// ============================================================================
// BlockConfig.cs - 블록 시각·연출 설정 (ScriptableObject)
// ============================================================================
// 설명: 드롭 속도, 기본 블록 스프라이트, 색상, 폭발 이펙트 프리팹을 에디터에서 할당하고 런타임에 참조합니다.
// 이유: 블록 종류나 연출을 바꿀 때 코드 수정 없이 에셋만 교체할 수 있게 합니다. CreateAssetMenu로 메뉴에서 생성 가능.
// ============================================================================

using System.Collections;
using System.Collections.Generic;
using Ninez.Board;
using UnityEngine;

namespace Ninez.Scriptable
{
    [CreateAssetMenu(menuName = "Bingle/Block Config", fileName = "BlockConfig.asset")]
    public class BlockConfig : ScriptableObject
    {
        public float[] dropSpeed;           // 낙하 칸 수별 재생 시간 (인덱스 0=1칸, ...)
        public Sprite[] basicBlockSprites; // breed별 스프라이트
        public Color[] blockColors;        // breed별 색 (폭발 파티클 등)
        public GameObject explosion;      // 블록 제거 시 생성할 파티클 프리팹

        /// <summary>퀘스트 타입별 폭발 오브젝트 반환. 현재는 CLEAR_SIMPLE만 사용, 확장 시 switch 확장</summary>
        public GameObject GetExplosionObject(BlockQuestType questType)
        {
            switch (questType)
            {
                case BlockQuestType.CLEAR_SIMPLE:
                    return Instantiate(explosion) as GameObject;
                default:
                    return Instantiate(explosion) as GameObject;
            }
        }

        /// <summary>breed에 대응하는 색. 폭발 파티클 startColor 설정에 사용</summary>
        public Color GetBlockColor(BlockBreed breed)
        {
            return blockColors[(int)breed];
        }
    }
}