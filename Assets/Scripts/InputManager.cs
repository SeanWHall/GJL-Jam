using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : BaseBehaviour
{
    public static InputManager Instance { get; private set; }
    
    public InputActionAsset InputAsset;

    public InputActionMap UI_Map;
    public InputAction    Pause_Action;

    public bool Pause_Value => Pause_Action.ReadValue<float>() > 0.5f;
    
    public override void OnEnable()
    {
        base.OnEnable();
        
        InputAsset.Enable();

        UI_Map = InputAsset.FindActionMap("UI");

        Pause_Action = UI_Map.FindAction("Pause");

        Instance = this;
    }
}

public class InputButton
{
    public InputAction  Action;
    public eButtonState State = eButtonState.None;

    public void Poll()
    {
        
    }
}

public enum eButtonState
{
    None    = 0,
    Pressed = 1,
    Held    = 2
}
