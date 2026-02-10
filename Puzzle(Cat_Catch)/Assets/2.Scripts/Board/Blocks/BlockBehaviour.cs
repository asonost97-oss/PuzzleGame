// ============================================================================
// BlockBehaviour.cs - 블록 GameObject의 시각 표현 및 제거 연출
// ============================================================================
// 설명: Block 데이터와 1:1로 연결되어 스프라이트 표시, 셀 크기 맞춤, 폭발 연출을 담당합니다.
// 이유: Model(Block)과 View(MonoBehaviour) 분리로, 로직 변경 없이 연출만 바꿀 수 있습니다.
// ============================================================================

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Ninez.Scriptable;

namespace Ninez.Board
{
    public class BlockBehaviour : MonoBehaviour
    {
        Block m_Block;                           // 연결된 데이터(Block)
        SpriteRenderer m_SpriteRenderer;        // 스프라이트 표시용
        [SerializeField] BlockConfig m_BlockConfig;  // 블록 이미지·폭발 이펙트 등 설정

        void Start()
        {
            m_SpriteRenderer = GetComponent<SpriteRenderer>();
            UpdateView(false);   // breed에 맞는 스프라이트 적용
            FitToCellSize(1f);   // 타일이 겹치지 않도록 스케일 조정
        }

        /// <summary>
        /// 블럭을 지정된 월드 유닛 크기(cellSize)에 맞춰 스케일합니다.
        /// 이유: 스프라이트 원본 크기와 관계없이 보드 한 칸에 정확히 맞춰 타일이 붙어 보이게 합니다.
        /// </summary>
        public void FitToCellSize(float cellSize)
        {
            if (m_SpriteRenderer == null || m_SpriteRenderer.sprite == null) return;
            Vector3 size = m_SpriteRenderer.sprite.bounds.size;
            if (size.x <= 0 || size.y <= 0) return;
            transform.localScale = new Vector3(cellSize / size.x, cellSize / size.y, 1f);
        }

        /// <summary>Block 데이터와 이 Behaviour를 연결. Block에서 set 시 호출됨</summary>
        internal void SetBlock(Block block)
        {
            m_Block = block;
        }

        /// <summary>
        /// Block의 type/breed에 맞게 스프라이트를 갱신합니다.
        /// 이유: 셔플 등으로 breed가 바뀌면 화면에 즉시 반영되어야 하므로 Block.breed setter에서 호출됩니다.
        /// </summary>
        public void UpdateView(bool bValueChanged)
        {
            if (m_Block.type == BlockType.EMPTY)
                m_SpriteRenderer.sprite = null;
            else if (m_Block.type == BlockType.BASIC)
                m_SpriteRenderer.sprite = m_BlockConfig.basicBlockSprites[(int)m_Block.breed];
        }

        /// <summary>블록 제거 연출 시작. Block.Destroy()에서 호출됨</summary>
        public void DoActionClear()
        {
            StartCoroutine(CoStartSimpleExplosion(true));
        }

        /// <summary>
        /// 축소 애니메이션 → 파티클 폭발 → GameObject 제거.
        /// 이유: 순간 삭제보다 짧은 연출을 넣어 플레이어가 "맞춤"을 인지하기 쉽게 합니다.
        /// </summary>
        IEnumerator CoStartSimpleExplosion(bool bDestroy = true)
        {
            yield return Action2D.Scale(transform, Core.Constants.BLOCK_DESTROY_SCALE, 4f);

            GameObject explosionObj = m_BlockConfig.GetExplosionObject(BlockQuestType.CLEAR_SIMPLE);
            ParticleSystem.MainModule newModule = explosionObj.GetComponent<ParticleSystem>().main;
            newModule.startColor = m_BlockConfig.GetBlockColor(m_Block.breed);

            explosionObj.SetActive(true);
            explosionObj.transform.position = this.transform.position;

            yield return new WaitForSeconds(0.1f);

            if (bDestroy)
                Destroy(gameObject);
            else
                Debug.Assert(false, "Unknown Action : GameObject No Destory After Particle");
        }
    }
}