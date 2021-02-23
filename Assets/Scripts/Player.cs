using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : Character
{
    public static Player Instance { get; private set; }
    
    public float               WalkSpeed = 5f;
    public float               MountDelay = 1f;
    public CharacterController Controller;

    public PlayerLocomotionState LocomotionState;
    public PlayerBoatState       BoatState;
    
    public override void OnEnable()
    {
        base.OnEnable();
        Instance = this;
        
        LocomotionState = new PlayerLocomotionState(this);
        BoatState       = new PlayerBoatState(this);

        ActiveState = LocomotionState;
    }

    public override void OnDisable()
    {
        base.OnDisable();
        Instance = null;
    }
}

public class PlayerState : CharacterState
{
    public Player              Player;
    public CharacterController Controller => Player.Controller;
    
    public PlayerState(Player Player) => this.Player = Player;
}

//Player is walking around
public class PlayerLocomotionState : PlayerState
{
    public PlayerLocomotionState(Player Player) : base(Player) { }
    
    public override void OnUpdate()
    {
        float Horizontal = InputManager.Character_Movement_Horizontal.Value;
        float Vertical   = InputManager.Character_Movement_Vertical.Value;

        Controller.SimpleMove(new Vector3(Horizontal, 0, Vertical) * Player.WalkSpeed);

        if (Boat.Instance.Dock != null && InputManager.Character_Mount.IsPressed)
        {
            PlayerBoatState BoatState   = Player.BoatState;
            float           CurrentTime = Time.time;

            if (CurrentTime > BoatState.NextAllowedMountChange)
                Player.ActiveState = BoatState;
        }
    }
}

//Player Driving Boat
public class PlayerBoatState : PlayerState
{
    public Boat  Boat => Boat.Instance;
    public float NextAllowedMountChange;
    
    public PlayerBoatState(Player Player) : base(Player) { }

    public override void OnEnter()
    {
        //Mount Player to Boat
        NextAllowedMountChange = Time.time + Player.MountDelay;
        
        Player.Controller.enabled = false;
        Player.transform.SetParent(Boat.PlayerSeat);
        Player.transform.localPosition = Vector3.zero;

        //Enable the Boat Physics
        Boat.Rigid.isKinematic = false;
    }

    public override void OnUpdate()
    {
        if(InputManager.Boat_Brake.IsPressed) Boat.Brake();
        else
        {
            if (InputManager.Boat_LeftOar.IsPressed)  Boat.RotateLeftOar(); //TODO: If Oar rotated, then play animation!
            if (InputManager.Boat_RightOar.IsPressed) Boat.RotateRightOar();
        }
        
        if (Boat.Dock != null && InputManager.Character_Mount.IsPressed)
        {
            float CurrentTime = Time.time;
            if (CurrentTime > NextAllowedMountChange)
                Player.ActiveState = Player.LocomotionState;
        }
    }

    public override void OnLeave()
    {
        //Unmount Player to boat
        NextAllowedMountChange = Time.time + Player.MountDelay;
        
        //Disable the Boat Physics
        Boat.Rigid.velocity        = Vector3.zero;
        Boat.Rigid.angularVelocity = Vector3.zero;
        Boat.Rigid.isKinematic     = true;
        
        Player.Controller.enabled = true;
        Player.transform.SetParent(null);
        Player.transform.position = Boat.Dock.ExitPoint.position;
    }
}