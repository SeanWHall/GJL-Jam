using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : Character
{
   public static Player Instance { get; private set; }
   
   public float Speed          = 5f;
   public float AirbourneSpeed = 3f;
   public float JumpHeight     = 6;
   public float JumpCooldown   = 0.5f;
   public float MountDelay     = 1f;
   
   public CharacterController Controller;
   public Animator            AnimController;

   public PlayerLocomotionState LocomotionState;
   public PlayerBoatState       BoatState;
   public PlayerJumpState       JumpState;
   
   public override void OnEnable()
   {
      base.OnEnable();
      Instance = this;

      LocomotionState = new PlayerLocomotionState(this);
      BoatState       = new PlayerBoatState(this);
      JumpState       = new PlayerJumpState(this);

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

//Player is currently jumping
public class PlayerJumpState : PlayerState
{
   public float NextJump;
   public float JumpVelocity;
   public PlayerJumpState(Player Player) : base(Player) {}

   public bool CanJump => Time.time >= NextJump;
   
   public override void OnEnter()
   {
      base.OnEnter();
      JumpVelocity = Player.JumpHeight * OrientatedInput.normalized.magnitude; //Shorten Jump Height if not moving
      AnimController.SetBool("IsJumping", true);
   }

   public override void OnUpdate()
   {
      base.OnUpdate();
      
      Vector3 Velocity = OrientatedInput;
      Player.transform.forward = Velocity;
      
      Velocity   *= Player.AirbourneSpeed;
      Velocity.y =  JumpVelocity;
      
      Controller.Move(Velocity * Time.deltaTime);
      
      JumpVelocity += Physics.gravity.y * Time.deltaTime;
      
      if (TimeInState > 0.5f && Controller.isGrounded)
         Player.ActiveState = Player.LocomotionState;
   }

   public override void OnLeave()
   {
      AnimController.SetBool("IsJumping", false);
      NextJump = Time.time + Player.JumpCooldown;
   }
}

//Player is walking around
public class PlayerLocomotionState : PlayerState
{
   public PlayerLocomotionState(Player Player) : base(Player) {}

   public override void OnUpdate()
   {
      Vector3 Movement = OrientatedInput;
      Vector3 Velocity = Vector3.zero;
      
      if (Movement.magnitude > 0)
      {
         if (Controller.isGrounded && Player.JumpState.CanJump && InputManager.Character_Jump.IsPressed)
         {
            Player.ActiveState = Player.JumpState; //Transition into the jump state
            return;
         }
         
         Velocity = Movement * Player.Speed;
         Player.transform.forward = Movement;
      }
      
      AnimController.SetFloat("Speed", Movement.magnitude);
      Controller.Move((Velocity + Physics.gravity) * Time.deltaTime);

      if (Boat.Instance.Dock != null && InputManager.Character_Mount.IsPressed)
      {
         PlayerBoatState BoatState   = Player.BoatState;
         float           CurrentTime = Time.time;

         if (CurrentTime > BoatState.NextAllowedMountChange)
         {
            AnimController.SetFloat("Speed", 0f);
            Player.ActiveState = BoatState;
         }
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
      base.OnEnter();
      
      //Mount Player to Boat
      NextAllowedMountChange = Time.time + Player.MountDelay;

      Player.Controller.enabled = false;
      Player.transform.SetParent(Boat.PlayerSeat);
      Player.transform.localPosition = Vector3.zero;
      Player.transform.rotation      = Boat.PlayerSeat.rotation;

      AnimController.SetBool("Sitting", true);
      AnimController.SetFloat("Speed", 0f);

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

   public override void OnAnimatorIK(int LayerIDx)
   {
      AnimController.SetIKPosition(AvatarIKGoal.LeftHand,  Boat.Left_Oar.IKPoint.position);
      AnimController.SetIKPosition(AvatarIKGoal.RightHand, Boat.Right_Oar.IKPoint.position);
      
      AnimController.SetIKPositionWeight(AvatarIKGoal.LeftHand, 1f);
      AnimController.SetIKPositionWeight(AvatarIKGoal.RightHand, 1f);
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