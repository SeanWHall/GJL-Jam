﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class Player : Character
{
   public static Player Instance { get; private set; }
   
   public float Speed          = 5f;
   public float AirbourneSpeed = 3f;
   public float JumpHeight     = 6;
   public float JumpCooldown   = 0.5f;
   public float MountDelay     = 1f;

   [NonSerialized] public Renderer[]          Renderers;
   [NonSerialized] public CharacterController Controller;
   [NonSerialized] public Animator            AnimController;
   [NonSerialized] public NavMeshObstacle     NavObstacle;
   [NonSerialized] public Vector3             LastSafePosition; //Respawn points
   
   public PlayerLocomotionState  LocomotionState;
   public PlayerBoatState        BoatState;
   public PlayerJumpState        JumpState;
   public PlayerChangeMountState MountState;

   //Just going to hard code it, normally I'd write an interaction system
   //But it aint worth it for one interaction
   [NonSerialized] public NPC          Following;
   [NonSerialized] public bool         LeftSide = false;
   [NonSerialized] public Vector3      HandGoal;
   [NonSerialized] public AvatarIKGoal IKGoal;
   
   public override void OnEnable()
   {
      base.OnEnable();
      Instance = this;

      Controller     = GetComponent<CharacterController>();
      AnimController = GetComponent<Animator>();
      NavObstacle    = GetComponent<NavMeshObstacle>();
      Renderers      = GetComponentsInChildren<Renderer>();
      
      LocomotionState = new PlayerLocomotionState(this);
      BoatState       = new PlayerBoatState(this);
      JumpState       = new PlayerJumpState(this);
      MountState      = new PlayerChangeMountState(this);

      ActiveState      = LocomotionState;
      LastSafePosition = transform.position;
   }

   public override void OnUpdate(float DeltaTime)
   {
      base.OnUpdate(DeltaTime);
      
      //If we have an NPC following the player, calculate the hand holding IK Goals
      if (Following == null)
         return;

      IKGoal                          = LeftSide ? AvatarIKGoal.LeftHand  : AvatarIKGoal.RightHand;
      Following.FollowingState.IKGoal = LeftSide ? AvatarIKGoal.RightHand : AvatarIKGoal.LeftHand;
      
      Vector3 NPCPosition    = Following.transform.position;
      Vector3 PlayerPosition = transform.position;
      Vector3 Direction      = Vector3.Normalize(PlayerPosition - NPCPosition);
      HandGoal               = NPCPosition + (Vector3.up * Following.HandHoldHeight) + (Direction * Vector3.Distance(NPCPosition, PlayerPosition) * 0.5f);
   }

   protected override void OnAnimatorIK(int LayerIDx)
   {
      base.OnAnimatorIK(LayerIDx);
      
      //If we have an NPC following the player, calculate the hand holding IK Goals
      if (Following == null)
         return;

      AnimController.SetIKPositionWeight(IKGoal, 1f);
      AnimController.SetIKPosition(IKGoal, HandGoal);
   }

   public override void OnDisable()
   {
      base.OnDisable();
      Instance = null;
   }

   public void Respawn()
   {
      HUD.Instance.FadeScreen(1.5f, 0.5f, OnFadeScreenEvent);
      void OnFadeScreenEvent(HUD.eFadeScreenEvent Event)
      {
         if (Event != HUD.eFadeScreenEvent.Middle)
            return;
         
         //Wait for the screen to be black, before moving the player
         Controller.enabled = false;
         transform.position = LastSafePosition;
         CameraController.Instance.PlayerState.UpdateCamera();
         Controller.enabled = true;
      }
   }

   public void SetRenderingState(bool State)
   {
      int Len = Renderers.Length;
      for (int i = 0; i < Len; i++)
         Renderers[i].enabled = State;
   }

   public static bool IsPartOfPlayer(Collider Col) => Instance != null && Col.GetComponentInChildren<Player>() == Instance || Col.GetComponentInParent<Player>() == Instance;
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

      if (Boat.Instance.Dock != null)
      {
         Player.LastSafePosition = Boat.Instance.Dock.ExitPoint.position;
         if (InputManager.Character_Mount.IsPressed && Player.MountState.AttemptMountChange(true))
            AnimController.SetFloat("Speed", 0f);
      }
   }
}

public class PlayerChangeMountState : PlayerState
{
   public float NextAllowedMountChange;
   public bool  Mounting;

   public bool AttemptMountChange(bool IsMounting)
   {
      if (Time.time < NextAllowedMountChange)
         return false;

      Mounting           = IsMounting;
      Player.ActiveState = this;
      return true;
   }
   
   public PlayerChangeMountState(Player player) : base(player) { }

   public override void OnEnter()
   {
      base.OnEnter();
      
      HUD.Instance.FadeScreen(1.5f, 0.5f, OnFadeScreenEvent);
   }
   
   void OnFadeScreenEvent(HUD.eFadeScreenEvent Event)
   {
      if (Event == HUD.eFadeScreenEvent.Middle)
      {
         //Do The Mounting Change logic
         if (Mounting) MountBoat();
         else          UnMountBoat();
         
         return;
      }
         
      //Fade has finished, transition to the next state
      Player.ActiveState = Mounting ? (CharacterState) Player.BoatState : Player.LocomotionState;
   }

   private void MountBoat()
   {
      //Mount Player to Boat
      NextAllowedMountChange = Time.time + Player.MountDelay;

      Player.Controller.enabled = false;
      Player.transform.SetParent(Boat.Instance.PlayerSeat);
      Player.transform.localPosition = Vector3.zero;
      Player.transform.rotation      = Boat.Instance.PlayerSeat.rotation;

      AnimController.SetBool("Sitting", true);
      AnimController.SetFloat("Speed", 0f);

      CameraController.Instance.ActiveState = CameraController.Instance.BoatState;

      //Enable the Boat Physics
      Boat.Instance.Rigid.isKinematic = false;
   }

   private void UnMountBoat()
   {
      //Unmount Player to boat
      NextAllowedMountChange = Time.time + Player.MountDelay;

      //Disable the Boat Physics
      Boat.Instance.Rigid.velocity        = Vector3.zero;
      Boat.Instance.Rigid.angularVelocity = Vector3.zero;
      Boat.Instance.Rigid.isKinematic     = true;

      CameraController.Instance.ActiveState = CameraController.Instance.PlayerState;

      AnimController.SetBool("Sitting", false);

      Player.Controller.enabled = true;
      Player.transform.SetParent(null);
      Player.transform.position = Boat.Instance.Dock.ExitPoint.position;
   }
}

//Player Driving Boat
public class PlayerBoatState : PlayerState
{
   public Boat  Boat => Boat.Instance;

   public PlayerBoatState(Player Player) : base(Player) { }
   
   public override void OnUpdate()
   {
      if (InputManager.Boat_Brake.IsPressed) Boat.Brake();
      else
      {
         if (InputManager.Boat_LeftOar.IsPressed)  Boat.RotateOar(Boat.Left_Oar);
         if (InputManager.Boat_RightOar.IsPressed) Boat.RotateOar(Boat.Right_Oar);
      }

      if (Boat.Dock != null && InputManager.Character_Mount.IsPressed)
         Player.MountState.AttemptMountChange(false);
   }

   public override void OnAnimatorIK(int LayerIDx)
   {
      AnimController.SetIKPosition(AvatarIKGoal.LeftHand,  Boat.Left_Oar.IKPoint.position);
      AnimController.SetIKPosition(AvatarIKGoal.RightHand, Boat.Right_Oar.IKPoint.position);
      
      AnimController.SetIKPositionWeight(AvatarIKGoal.LeftHand, 1f);
      AnimController.SetIKPositionWeight(AvatarIKGoal.RightHand, 1f);
   }
}