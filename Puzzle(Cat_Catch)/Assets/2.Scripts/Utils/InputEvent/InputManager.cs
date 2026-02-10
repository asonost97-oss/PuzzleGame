// ============================================================================
// InputManager.cs - 터치/마우스 입력을 보드 좌표로 변환하고 스와이프 방향 판정
// ============================================================================
// 설명: 플랫폼에 따라 MouseHandler 또는 TouchHandler를 쓰고, 스크린 좌표를 컨테이너 기준 로컬 좌표로 바꿔 Stage에서 사용합니다.
// 이유: StageController가 "보드 기준 좌표"와 "스와이프 방향"만 알면 되므로, 입력 장치와 좌표 변환을 여기서 처리합니다.
// ============================================================================

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InputManager
{
    Transform m_Container;

#if UNITY_ANDROID && !UNITY_EDITOR
    IInputHandlerBase m_InputHandler = new TouchHandler();
#else
    IInputHandlerBase m_InputHandler = new MouseHandler();
#endif
    public InputManager(Transform container)
    {
        m_Container = container;
    }

    public bool isTouchDown => m_InputHandler.isInputDown;
    public bool isTouchUp => m_InputHandler.isInputUp;
    public Vector2 touchPosition => m_InputHandler.inputPosition;
    /// <summary>현재 입력 위치를 보드(컨테이너) 기준 로컬 좌표로 변환한 값. IsInsideBoard/IsOnValideBlock에서 사용</summary>
    public Vector2 touch2BoardPosition => TouchToPosition(m_InputHandler.inputPosition);

    /// <summary>스크린 픽셀 좌표 → 월드 → 컨테이너 로컬로 변환. 보드 이동 시에도 보드 기준으로 계산 가능</summary>
    Vector2 TouchToPosition(Vector3 vtInput)
    {
        Vector3 vtMousePosW = Camera.main.ScreenToWorldPoint(vtInput);
        Vector3 vtContainerLocal = m_Container.transform.InverseTransformPoint(vtMousePosW);
        return vtContainerLocal;
    }

    /// <summary>드래그 시작·끝 좌표로 스와이프 방향(UP/DOWN/LEFT/RIGHT/NA) 반환</summary>
    public Swipe EvalSwipeDir(Vector2 vtStart, Vector2 vtEnd)
    {
        return TouchEvaluator.EvalSwipeDir(vtStart, vtEnd);
    }
}