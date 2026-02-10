// ============================================================================
// Cell.cs - 보드 한 칸의 셀 데이터 (타입, GameObject 연결)
// ============================================================================
// 설명: 각 칸이 빈칸/기본/장애물 등 어떤 타입인지 저장하고, 해당 칸의 배경 GameObject와 연결합니다.
// 이유: 블록 배치·이동 가능 여부는 셀 타입으로 판단하고, 시각은 CellBehaviour가 담당해 역할을 나눕니다.
// ============================================================================

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Cell
{
    protected CellType m_CellType;
    public CellType type
    {
        get { return m_CellType; }
        set { m_CellType = value; }
    }

    protected CellBehaviour m_CellBehaviour;
    public CellBehaviour cellBehaviour
    {
        get { return m_CellBehaviour; }
        set
        {
            m_CellBehaviour = value;
            m_CellBehaviour.SetCell(this);
        }
    }

    public Cell(CellType cellType)
    {
        m_CellType = cellType;
    }

    /// <summary>Cell Prefab으로 GameObject를 생성해 컨테이너 자식으로 넣고, CellBehaviour 참조를 저장합니다.</summary>
    public Cell InstantiateCellObj(GameObject cellPrefab, Transform containerObj)
    {
        GameObject newObj = Object.Instantiate(cellPrefab, new Vector3(0, 0, 0), Quaternion.identity);
        newObj.transform.parent = containerObj;
        this.cellBehaviour = newObj.transform.GetComponent<CellBehaviour>();
        return this;
    }

    /// <summary>연결된 셀 GameObject를 지정 좌표로 이동 (보드 배치 시 사용)</summary>
    public void Move(float x, float y)
    {
        cellBehaviour.transform.position = new Vector3(x, y);
    }

    /// <summary>장애물(빈칸 등)이면 true. 블록이 이 칸을 "막힌 칸"으로 간주할 때 사용</summary>
    public bool IsObstracle()
    {
        return type == CellType.EMPTY;
    }
}

