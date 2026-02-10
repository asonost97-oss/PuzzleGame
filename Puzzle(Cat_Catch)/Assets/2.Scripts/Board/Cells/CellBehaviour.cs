// ============================================================================
// CellBehaviour.cs - 셀 GameObject의 시각 표현
// ============================================================================
// 설명: Cell 데이터와 1:1 연결되어 배경 스프라이트 표시와 셀 크기 맞춤을 담당합니다.
// 이유: Block과 마찬가지로 데이터(Cell)와 뷰(MonoBehaviour)를 나누어 연출 변경을 쉽게 합니다.
// ============================================================================

using System.Collections;
using System.Collections.Generic;
using Ninez.Board;
using UnityEngine;

namespace Ninez.Board
{
    public class CellBehaviour : MonoBehaviour
    {
        Cell m_Cell;
        SpriteRenderer m_SpriteRenderer;

        void Start()
        {
            m_SpriteRenderer = GetComponent<SpriteRenderer>();
            UpdateView(false);
            FitToCellSize(1f);
        }

        /// <summary>셀을 지정된 월드 유닛 크기에 맞춰 스케일해 타일이 겹치지 않게 합니다.</summary>
        public void FitToCellSize(float cellSize)
        {
            if (m_SpriteRenderer == null || m_SpriteRenderer.sprite == null) return;
            Vector3 size = m_SpriteRenderer.sprite.bounds.size;
            if (size.x <= 0 || size.y <= 0) return;
            transform.localScale = new Vector3(cellSize / size.x, cellSize / size.y, 1f);
        }

        void Update() { }

        /// <summary>Cell 데이터와 이 Behaviour 연결. Cell.cellBehaviour setter에서 호출</summary>
        public void SetCell(Cell cell)
        {
            m_Cell = cell;
        }

        /// <summary>Cell 타입에 맞게 스프라이트 갱신. EMPTY면 스프라이트 제거 (배경 안 그리기)</summary>
        public void UpdateView(bool bValueChanged)
        {
            if (m_Cell.type == CellType.EMPTY)
                m_SpriteRenderer.sprite = null;
        }
    }
}