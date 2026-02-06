using System;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 새 Input System에서 터치/마우스 위치(Select)와 클릭/탭(Fire)을 읽어서
/// Match3Manager에 전달. PlayerInput 컴포넌트와 Input Actions 에셋(Select, Fire 액션) 필요.
/// </summary>
[RequireComponent(typeof(PlayerInput))]
public class InputReader : MonoBehaviour
{
    /// <summary> 위치를 주는 액션 이름. 없으면 "Point"도 시도. </summary>
    [Tooltip("터치/마우스 위치를 주는 액션 (없으면 Select → Point 순으로 찾음)")]
    [SerializeField] string selectActionName = "Select";
    /// <summary> 클릭/탭을 주는 액션 이름. 없으면 "Click"도 시도. </summary>
    [Tooltip("클릭/탭을 주는 액션 (없으면 Fire → Click 순으로 찾음)")]
    [SerializeField] string fireActionName = "Fire";

    PlayerInput playerInput;
    InputAction selectAction;
    InputAction fireAction;
    /// <summary> Select·Fire 액션을 모두 찾았을 때만 true. false면 Selected는 0, Fire 구독 안 함. </summary>
    bool valid;

    /// <summary> 클릭/탭이 발생했을 때 발생. Match3Manager가 OnSelectGem에 구독. </summary>
    public event Action Fire;

    /// <summary> 현재 터치/마우스 위치 (스크린 좌표). 액션 없으면 Vector2.zero. </summary>
    public Vector2 Selected => valid && selectAction != null ? selectAction.ReadValue<Vector2>() : Vector2.zero;

    void Start()
    {
        playerInput = GetComponent<PlayerInput>();
        if (playerInput?.actions == null)
        {
            Debug.LogError("InputReader: PlayerInput 또는 Actions가 없습니다.");
            return;
        }

        selectAction = playerInput.actions.FindAction(selectActionName, false)
            ?? playerInput.actions.FindAction("Point", false);
        fireAction = playerInput.actions.FindAction(fireActionName, false)
            ?? playerInput.actions.FindAction("Click", false);

        if (selectAction == null)
            Debug.LogError($"InputReader: 액션 '{selectActionName}' 또는 'Point'를 찾을 수 없습니다. Input Actions에 위치용 Vector2 액션을 추가하세요.");
        if (fireAction == null)
            Debug.LogError($"InputReader: 액션 '{fireActionName}' 또는 'Click'을 찾을 수 없습니다. Input Actions에 클릭용 Button 액션을 추가하세요.");

        valid = selectAction != null && fireAction != null;
        if (valid)
            fireAction.performed += OnFire;
    }

    void OnDestroy()
    {
        if (fireAction != null)
            fireAction.performed -= OnFire;
    }

    /// <summary> Fire 액션 performed 시 호출. Fire 이벤트 발생. </summary>
    void OnFire(InputAction.CallbackContext obj) => Fire?.Invoke();
}
