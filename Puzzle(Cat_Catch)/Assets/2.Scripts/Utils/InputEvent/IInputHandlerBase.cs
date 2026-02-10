// ============================================================================
// IInputHandlerBase.cs - 터치/마우스 입력을 추상화하는 인터페이스
// ============================================================================
// 설명: "한 번 누름", "한 번 뗌", "현재 입력 좌표"를 플랫폼(에디터=마우스, 안드로이드=터치)에 따라 다르게 구현할 수 있게 합니다.
// 이유: InputManager가 이 인터페이스만 의존하면, 플랫폼별 분기는 Handler 교체로만 처리할 수 있습니다.
// ============================================================================

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IInputHandlerBase
{
    bool isInputDown { get; }
    bool isInputUp { get; }
    Vector2 inputPosition { get; }  // 스크린(픽셀) 좌표
}
