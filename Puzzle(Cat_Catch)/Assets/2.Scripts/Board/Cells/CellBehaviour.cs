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

        // Start is called before the first frame update
        void Start()
        {
            m_SpriteRenderer = GetComponent<SpriteRenderer>();

            UpdateView(false);
            FitToCellSize(1f);
        }

        /// <summary>
        /// 셀을 지정된 월드 유닛 크기에 맞춰 스케일하여 타일이 서로 붙어 보이게 한다.
        /// </summary>
        public void FitToCellSize(float cellSize)
        {
            if (m_SpriteRenderer == null || m_SpriteRenderer.sprite == null) return;
            Vector3 size = m_SpriteRenderer.sprite.bounds.size;
            if (size.x <= 0 || size.y <= 0) return;
            transform.localScale = new Vector3(cellSize / size.x, cellSize / size.y, 1f);
        }

        // Update is called once per frame
        void Update()
        {

        }

        public void SetCell(Cell cell)
        {
            m_Cell = cell;
        }

        /// <summary>
        /// 참조하고 있는 Cell의 정보를 반영하여 Cell GameObject에 반영한다
        /// ex) Cell 종류에 따른 Sprite 종류 업데이트
        /// 생성자 또는 플레이도중에 Cell Type이 변경될 때 호출된다.
        /// </summary>
        /// <param name="bValueChanged">플레이 도중에 Type이 변경되는 경우 true, 그렇지 않은 경우 false</param>
        public void UpdateView(bool bValueChanged)
        {
            if (m_Cell.type == CellType.EMPTY)
            {
                m_SpriteRenderer.sprite = null;
            }
        }

    }
}