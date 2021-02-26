using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : Character
{
   public static Player Instance { get; private set; }

   public float               Speed         = 5f;
   public float               RotationSpeed = 6f;
   public float               MountDelay    = 1f;
   public CharacterController Controller;
   public Animator            AnimController;

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
   public CharacterController Controller     => Player.Controller;
   public Animator            AnimController => Player.AnimController;

   public Vector3 OrientatedInput
   {
      get
      {
         Vector2    Input           = InputManager.Character_Movement.Value;
         Quaternion Cam_Orientation = Quaternion.AngleAxis(CameraController.WorldOrientation, Vector3.up);
         return Cam_Orientation * Vector3.ClampMagnitude(new Vector3(Input.x, 0, Input.y), 1.0f);
      }
   }
   
   public PlayerState(Player Player) => this.Player = Player;
}

//Player is walking around
public class PlayerLocomotionState : PlayerState
{
   public PlayerLocomotionState(Player Player) : base(Player) {}

   public override void OnUpdate()
   {
      Vector3 Movement         = OrientatedInput;
      Vector3 PlayerFwd        = Player.transform.forward;
      PlayerFwd                =  Vector3.Normalize(Vector3.Lerp(PlayerFwd, Movement, Player.RotationSpeed * Time.deltaTime));
      Player.transform.forward =  PlayerFwd;
      PlayerFwd                *= Movement.magnitude * Player.Speed;
      
      AnimController.SetFloat("Speed", Movement.magnitude);
      Controller.SimpleMove(PlayerFwd);

      if (Boat.Instance.Dock != null && InputManager.Character_Mount.IsPressed)
      {
         PlayerBoatState BoatState   = Player.BoatState;
         float           CurrentTime = Time.time;

         if (CurrentTime > BoatState.NextAllowedMountChange)
            Player.ActiveState = BoatState;
      }
   }

   public override void OnLeave()
   {
      AnimController.SetFloat("Speed", 0f);
   }
}

//Player Driving Boat
public class PlayerBoatState : PlayerState
{
   public Boat  Boat => Boat.Instance;
   public float NextAllowedMountChange;

   public PlayerBoatState(Player Player) : base(Player)
   {
   }

   public override void OnEnter()
   {
      //Mount Player to Boat
      NextAllowedMountChange = Time.time + Player.MountDelay;

      Player.Controller.enabled = false;
      Player.transform.SetParent(Boat.PlayerSeat);
      Player.transform.localPosition = Vector3.zero;

      AnimController.SetBool("Sitting", true);

      //Enable the Boat Physics
      Boat.Rigid.isKinematic = false;
   }

   public override void OnUpdate()
   {
      if (InputManager.Boat_Brake.IsPressed) Boat.Brake();
      else
      {
         if (InputManager.Boat_LeftOar.IsPressed) Boat.RotateOar(Boat.Left_Oar);
         if (InputManager.Boat_RightOar.IsPressed) Boat.RotateOar(Boat.Right_Oar);
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


      AnimController.SetBool("Sitting", false);

      Player.Controller.enabled = true;
      Player.transform.SetParent(null);
      Player.transform.position = Boat.Dock.ExitPoint.position;
   }
}