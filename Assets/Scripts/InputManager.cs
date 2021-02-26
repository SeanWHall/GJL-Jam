using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

public class InputManager : BaseBehaviour
{
    public static InputManager Instance { get; private set; }

    public override eUpdateFlags UpdateFlags => eUpdateFlags.RequireUpdate | eUpdateFlags.WhilePaused;

    public InputActionAsset InputAsset;
    
    public InputButton UI_Pause;
    
    public InputVector2 Character_Movement;
    public InputButton  Character_Mount;
    public InputButton  Character_Jump;
    
    public InputButton Boat_LeftOar;
    public InputButton Boat_RightOar;
    public InputButton Boat_Brake;

    private List<InputWrapper> _Buttons = new List<InputWrapper>();
    
    public override void OnEnable()
    {
        base.OnEnable();
        
        InputAsset.Enable();

        InputActionMap UI_Map = InputAsset.FindActionMap("UI");
        UI_Pause = AddButton(new InputButton(UI_Map.FindAction("Pause")));
        
        InputActionMap Boat_Map = InputAsset.FindActionMap("Boat");
        Boat_Brake    = AddButton(new InputButton(Boat_Map.FindAction("Brake")));
        Boat_LeftOar  = AddButton(new InputButton(Boat_Map.FindAction("Left Oar")));
        Boat_RightOar = AddButton(new InputButton(Boat_Map.FindAction("Right Oar")));
        
        
        InputActionMap Player_Map = InputAsset.FindActionMap("Player");
        Character_Movement = AddButton(new InputVector2(Player_Map.FindAction("Movement")));
        Character_Mount    = AddButton(new InputButton(Player_Map.FindAction("Mount")));
        Character_Jump     = AddButton(new InputButton(Player_Map.FindAction("Jump")));
        
        Instance = this;
    }

    public override void OnUpdate(float DeltaTime)
    {
        foreach(var Button in _Buttons)
            Button.Poll();
    }

    private T AddButton<T>(T Button) where T : InputWrapper
    {
        if (Button != null)
            _Buttons.Add(Button);
        return Button;
    }
}

public abstract class InputWrapper
{
    public InputAction Action;

    public InputWrapper(InputAction Action) => this.Action = Action;

    public abstract void Poll();
}

public class InputVector2 : InputWrapper
{
    public Vector2 Value;
    public InputVector2(InputAction Action) : base(Action) {}
    public override void Poll() => Value = Action.ReadValue<Vector2>();
}

public class InputAxis : InputWrapper
{
    public float Value;
    public InputAxis(InputAction Action) : base(Action) {}
    public override void Poll() => Value = Action.ReadValue<float>();
}

public class InputButton : InputWrapper
{
    public eButtonState State = eButtonState.None;

    public bool IsPressedOrHeld => State == eButtonState.Pressed || State == eButtonState.Held;
    public bool IsPressed       => State == eButtonState.Pressed;
    public bool IsHeld          => State == eButtonState.Held;
    
    public InputButton(InputAction Action) : base(Action) {}
    
    public override void Poll()
    {
        float Actuation = Action.ReadValue<float>();
        if (Actuation >= 0.1f)
            State = State == eButtonState.Pressed ? eButtonState.Held : eButtonState.Pressed;
        else
            State = eButtonState.None;
    }

    public static implicit operator eButtonState(InputButton Button) => Button != null ? Button.State : eButtonState.None;
}

public enum eButtonState
{
    None    = 0,
    Pressed = 1,
    Held    = 2
}