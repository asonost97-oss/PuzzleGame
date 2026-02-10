// ============================================================================
// TouchHandler.cs - 모바일 터치 입력 구현
// ============================================================================
// 설명: IInputHandlerBase를 첫 번째 터치(0번)로 구현. Android 빌드에서 InputManager가 이 핸들러를 사용합니다.
// 이유: 터치 Began/Ended와 position을 마우스와 동일한 인터페이스로 제공해 StageController 코드를 공유합니다.
// ============================================================================

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TouchHandler : IInputHandlerBase
{
    bool IInputHandlerBase.isInputDown => Input.GetTouch(0).phase == TouchPhase.Began;
    bool IInputHandlerBase.isInputUp => Input.GetTouch(0).phase == TouchPhase.Ended;
    Vector2 IInputHandlerBase.inputPosition => Input.GetTouch(0).position;
}
