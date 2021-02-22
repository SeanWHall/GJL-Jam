using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : BaseBehaviour
{
    public static Player Instance { get; private set; }
    
    public float               WalkSpeed = 5f;
    public CharacterController Controller;

    public bool  IsBeingControlled = true;
    public float MountDelay;
    
    public override eUpdateFlags UpdateFlags => eUpdateFlags.RequireUpdate;

    public override void OnEnable()
    {
        base.OnEnable();
        Instance = this;
    }

    public override void OnUpdate(float DeltaTime)
    {
        if (MountDelay > 0f)
            MountDelay -= DeltaTime;
        
        if (!IsBeingControlled)
            return;

        float Horizontal = InputManager.Character_Movement_Horizontal.Value;
        float Vertical   = InputManager.Character_Movement_Vertical.Value;

        Controller.SimpleMove(new Vector3(Horizontal, 0, Vertical) * WalkSpeed);
        
        if(BoatProximity.IsPlayerClose && InputManager.Character_Mount.IsPressed)
            Boat.Instance.Mount();
    }

    public override void OnDisable()
    {
        base.OnDisable();
        Instance = null;
    }
}
