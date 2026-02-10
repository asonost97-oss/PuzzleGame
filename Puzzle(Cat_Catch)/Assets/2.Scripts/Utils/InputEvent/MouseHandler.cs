// ============================================================================
// MouseHandler.cs - 에디터/PC용 마우스 입력 구현
// ============================================================================
// 설명: IInputHandlerBase를 마우스(클릭=Fire1)로 구현. 에디터와 PC 빌드에서 InputManager가 이 핸들러를 사용합니다.
// 이유: UNITY_ANDROID가 아니거나 에디터일 때 터치 대신 마우스로 동일한 인터페이스를 제공합니다.
// ============================================================================

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MouseHandler : IInputHandlerBase
{
    bool IInputHandlerBase.isInputDown => Input.GetButtonDown("Fire1");
    bool IInputHandlerBase.isInputUp => Input.GetButtonUp("Fire1");
    Vector2 IInputHandlerBase.inputPosition => Input.mousePosition;
}
