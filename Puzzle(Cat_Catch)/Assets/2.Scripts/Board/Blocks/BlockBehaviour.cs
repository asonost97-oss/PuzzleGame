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
public class BlockBehaviour : MonoBehaviour
{
    Block m_Block;
    SpriteRenderer m_SpriteRenderer;
    [SerializeField] BlockConfig m_BlockConfig;

    void Start()
    {
        m_SpriteRenderer = GetComponent<SpriteRenderer>();
        UpdateView(false);
        FitToCellSize(1f);
    }

    /// <summary>
    /// 블럭을 지정된 월드 유닛 크기(cellSize)에 맞춰 스케일합니다.
    /// </summary>
    public void FitToCellSize(float cellSize)
    {
        if (m_SpriteRenderer == null || m_SpriteRenderer.sprite == null) return;
        Vector3 size = m_SpriteRenderer.sprite.bounds.size;
        if (size.x <= 0 || size.y <= 0) return;
        transform.localScale = new Vector3(cellSize / size.x, cellSize / size.y, 1f);
    }

    internal void SetBlock(Block block)
    {
        m_Block = block;
    }

    public void UpdateView(bool bValueChanged)
    {
        if (m_Block.type == BlockType.EMPTY)
            m_SpriteRenderer.sprite = null;
        else if (m_Block.type == BlockType.BASIC)
            m_SpriteRenderer.sprite = m_BlockConfig.basicBlockSprites[(int)m_Block.breed];
    }

    public void DoActionClear()
    {
        StartCoroutine(CoStartSimpleExplosion(true));
    }

    IEnumerator CoStartSimpleExplosion(bool bDestroy = true)
    {
        yield return Action2D.Scale(transform, Constants.BLOCK_DESTROY_SCALE, 4f);

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