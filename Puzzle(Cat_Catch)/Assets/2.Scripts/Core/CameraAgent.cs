// ============================================================================
// CameraAgent.cs - 보드 너비에 맞춘 카메라 뷰 설정
// ============================================================================
// 설명: 지정한 보드 단위 너비(m_BoardUnit)가 화면 가로에 맞도록 Orthographic Size를 계산합니다.
// 이유: StageController.FitCameraToBoard와 별도로, 고정 보드 크기 씬에서 카메라만 씬에 두고 사용할 때 유용합니다.
// ============================================================================

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraAgent : MonoBehaviour
{
    [SerializeField] Camera m_TargetCamera;
    [SerializeField] float m_BoardUnit;  // 화면에 보여줄 보드의 "가로 길이" (월드 유닛)

    void Start()
    {
        m_TargetCamera.orthographicSize = m_BoardUnit / m_TargetCamera.aspect;
    }
}
