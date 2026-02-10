// ============================================================================
// StageController.cs - 스테이지 씬의 진입점: 초기화, 입력 처리, 스와이프 전달
// ============================================================================
// 설명: 씬 로드 시 스테이지를 빌드·구성하고, 매 프레임 터치/마우스 입력을 받아 유효한 스와이프만 ActionManager에 넘깁니다.
// 이유: MonoBehaviour이므로 씬에 하나만 두고, 입력과 게임 로직(ActionManager/Stage)을 연결하는 역할만 합니다.
// ============================================================================

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StageController : MonoBehaviour
{
    bool m_bInit;
    Stage m_Stage;
    InputManager m_InputManager;
    ActionManager m_ActionManager;

    bool m_bTouchDown;
    BlockPos m_BlockDownPos;
    Vector3 m_ClickPos;

    [SerializeField] Transform m_Container;
    [SerializeField] GameObject m_CellPrefab;
    [SerializeField] GameObject m_BlockPrefab;
    [SerializeField] Camera m_StageCamera;

    void Start()
    {
        InitStage();
    }

    private void Update()
    {
        if (!m_bInit) return;
        OnInputHandler();
    }

    void InitStage()
    {
        if (m_bInit) return;
        m_bInit = true;
        m_InputManager = new InputManager(m_Container);
        BuildStage();
    }

    void BuildStage()
    {
        m_Stage = StageBuilder.BuildStage(nStage: 1);
        m_ActionManager = new ActionManager(m_Container, m_Stage);
        m_Stage.ComposeStage(m_CellPrefab, m_BlockPrefab, m_Container);
        FitCameraToBoard();
    }

    void FitCameraToBoard()
    {
        Camera cam = m_StageCamera != null ? m_StageCamera : Camera.main;
        if (cam == null || !cam.orthographic) return;

        float boardH = m_Stage.maxRow;
        float boardW = m_Stage.maxCol;
        float aspect = cam.aspect;
        cam.orthographicSize = Mathf.Max(boardH * 0.5f, boardW * 0.5f / aspect);
    }

    void OnInputHandler()
    {
        if (!m_bTouchDown && m_InputManager.isTouchDown)
        {
            Vector2 point = m_InputManager.touch2BoardPosition;
            if (!m_Stage.IsInsideBoard(point)) return;

            BlockPos blockPos;
            if (m_Stage.IsOnValideBlock(point, out blockPos))
            {
                m_bTouchDown = true;
                m_BlockDownPos = blockPos;
                m_ClickPos = point;
            }
        }
        else if (m_bTouchDown && m_InputManager.isTouchUp)
        {
            Vector2 point = m_InputManager.touch2BoardPosition;
            Swipe swipeDir = m_InputManager.EvalSwipeDir(m_ClickPos, point);

            if (swipeDir != Swipe.NA)
                m_ActionManager.DoSwipeAction(m_BlockDownPos.row, m_BlockDownPos.col, swipeDir);

            m_bTouchDown = false;
        }
    }
}
