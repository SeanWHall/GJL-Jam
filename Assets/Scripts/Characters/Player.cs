using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Audio; // Tom

public class Player : Character
{
   private List<IInteractable> _Interactables = new List<IInteractable>();
   
   public static Player Instance { get; private set; }
   
   [Header("Settings")]
   public float     Speed            = 5f;
   public float     CarryingSpeedMod = 0.75f;
   public float     InteractionDelay = 1f;
   public float     AirbourneSpeed   = 3f;
   public float     JumpHeight       = 6;
   public float     JumpCooldown     = 0.5f;
   public float     MountDelay       = 1f;
   
   public Transform PickupMountPoint;
   
   [Header("Carry Points")]
   public Transform Carry_Hand_LeftIK;
   public Transform Carry_Hand_RightIK;
   public Transform Carry_Foot_LeftIK;
   public Transform Carry_Foot_RightIK;
   public Transform Carry_MountPoint;
   
   [Header("Audio")]
   public AudioClip   JumpSound; // Tom
   public AudioClip   BoatEnter; // Tom
   public AudioClip   BoatExit; // Tom
   public AudioSource PlayerAudioSource; // Tom

   [NonSerialized] public Renderer[]          Renderers;
   [NonSerialized] public CharacterController Controller;
   [NonSerialized] public NavMeshObstacle     NavObstacle;
   [NonSerialized] public Vector3             LastSafePosition; //Respawn points
   [NonSerialized] public float               NextInteractionTime;
   
   public PlayerLocomotionState    LocomotionState;
   public PlayerBoatState          BoatState;
   public PlayerJumpState          JumpState;
   public PlayerChangeMountState   MountState;
   public PlayerDialogueState      DialogueState;
   public PlayerLeadingNPCState    LeadingNPCState;
   public PlayerCarryNPCState      CarryNPCState;
   public PlayerHoldingObjectState HoldingObjectState;
   
   public override void OnEnable()
   {
      base.OnEnable();
      Instance = this;

      Controller     = GetComponent<CharacterController>();
      AnimController = GetComponent<Animator>();
      NavObstacle    = GetComponent<NavMeshObstacle>();
      Renderers      = GetComponentsInChildren<Renderer>();
      
      LocomotionState    = new PlayerLocomotionState(this);
      BoatState          = new PlayerBoatState(this);
      JumpState          = new PlayerJumpState(this);
      MountState         = new PlayerChangeMountState(this);
      DialogueState      = new PlayerDialogueState(this);
      LeadingNPCState    = new PlayerLeadingNPCState(this);
      CarryNPCState      = new PlayerCarryNPCState(this);
      HoldingObjectState = new PlayerHoldingObjectState(this);

      ActiveState      = LocomotionState;
      LastSafePosition = transform.position;

      PlayerAudioSource = GetComponent<AudioSource>(); // Tom
   }

   public override void OnEnterDialogue(Character[] Participants) => ActiveState = DialogueState;

   public override void OnLeaveDialogue(Character[] Participants)
   {
      PlayerState    State = LocomotionState;
      NPC_Followable NPC   = GetCharacterOutOfArray<NPC_Followable>(Participants);

      if (NPC != null)
      {
         //We've interacted with a NPC that wants to follow US!
         int Follow_IDx = NPC.GetDialogueBoolIDx("Follow");
         int Carry_IDx  = NPC.GetDialogueBoolIDx("Carry");

         if (Follow_IDx != -1 && NPC.Bools[Follow_IDx].Value)
         {
            //NPC Is following Us, use leading locomotion
            LeadingNPCState.NPC = NPC;
            State               = LeadingNPCState;
         }
         else if (Carry_IDx != -1 && NPC.Bools[Carry_IDx].Value)
         {
            CarryNPCState.NPC = NPC;
            State             = CarryNPCState; //NPC is on our back, use carrying locomotion
         }
      }
      
      ActiveState = State; 
   }

   public override void OnUpdate(float DeltaTime)
   {
      base.OnUpdate(DeltaTime);

      HandleInteraction();
   }

   public void HandleInteraction()
   {
      if (!((PlayerState)ActiveState).CanInteract || Time.time < NextInteractionTime)
      {
         HUD.Instance.HideInteractionUI();
         return;
      }

      int           Mask                = LayerMask.GetMask("Default"); //Check if there is anything on layer default, between the player and the interactable
      Vector3       Player_Position     = transform.position;
      IInteractable Closest_Interaction = null;
      float         Closest_Dist        = float.PositiveInfinity;
      // Currently can't interact with anything
      int Interactables_Len = GetBehaviours(_Interactables);
      for (int i = 0; i < Interactables_Len; i++)
      {
         IInteractable Interactable = _Interactables[i];
         if(Interactable == null || !Interactable.CanInteract(this))
            continue; //make sure that the player can interact with this
         
         Vector3 Interaction_Position = Interactable.Position;
         float   Interaction_Dist     = Vector3.Distance(Player_Position, Interaction_Position);
         
         if(Interaction_Dist > Interactable.InteractionDistance || Closest_Interaction != null && (Interaction_Dist > Closest_Dist || Interactable.InteractionPriority <= Closest_Interaction.InteractionPriority))
            continue; //Player is too far away from the interaction or there is another one closer

         //Now check that the player is able to see the interaction
         if (Physics.Linecast(Player_Position, Interaction_Position, Mask, QueryTriggerInteraction.Ignore))
            continue;

         //Update the closest interactable with this one
         Closest_Interaction = Interactable;
         Closest_Dist        = Interaction_Dist;
      }

      if (Closest_Interaction == null)
      {
         //Nothing to interact with
         HUD.Instance.HideInteractionUI();
         return;
      }
      
      //Show the interaction UI
      HUD.Instance.ShowInteractionUI(Closest_Interaction);

      if (InputManager.Character_Interact.IsPressed)
      {
         Closest_Interaction.OnInteract(this);
         HUD.Instance.HideInteractionUI();
         NextInteractionTime = Time.time + InteractionDelay;
      }
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
   public Player              Player     => (Player) Character;
   public CharacterController Controller => Player.Controller;

   public virtual bool CanInteract => false;
   
   public Vector3 OrientatedInput
   {
      get
      {
         Vector2    Input           = InputManager.Character_Movement.Value;
         Quaternion Cam_Orientation = Quaternion.AngleAxis(CameraController.WorldOrientation, Vector3.up);
         return Cam_Orientation * Vector3.ClampMagnitude(new Vector3(Input.x, 0, Input.y), 1.0f);
      }
   }
   
   public PlayerState(Character Character) : base(Character) {}
}

public class PlayerDialogueState : PlayerState
{
   public PlayerDialogueState(Player Player) : base(Player) { }

   public override void OnEnter()
   {
      base.OnEnter();
      
      //TODO: Face towards the Participants of the dialogue
   }
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
        Debug.Log("Play Jump Sound"); // Tom
        //PlayerAudioSource.clip = JumpSound; // Tom
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
        Debug.Log("Landing Sound");
   }
}

//Player is walking around
public class PlayerLocomotionState : PlayerState
{
   public virtual  float Speed       => Player.Speed;
   public virtual  bool  CanJump     => true;
   public override bool  CanInteract => true;

   public PlayerLocomotionState(Player Player) : base(Player) {}

   public override void OnUpdate()
   {
      Vector3 Movement = OrientatedInput;
      Vector3 Velocity = Vector3.zero;
      
      if (Movement.magnitude > 0)
      {
         if (CanJump && Controller.isGrounded && Player.JumpState.CanJump && InputManager.Character_Jump.IsPressed)
         {
            Player.ActiveState = Player.JumpState; //Transition into the jump state
            return;
         }
         
         Velocity = Movement * Speed;
         Player.transform.forward = Movement;
      }
      
      AnimController.SetFloat("Speed", Movement.magnitude);
      Controller.Move((Velocity + Physics.gravity) * Time.deltaTime);

      if (Boat.Instance != null && Boat.Instance.Dock != null)
         Player.LastSafePosition = Boat.Instance.Dock.Player_ExitPoint.position;
   }

   public override void OnLeave()
   {
      base.OnLeave();
      AnimController.SetFloat("Speed", 0f);
   }
}

public class PlayerChangeMountState : PlayerState
{
   public float          NextAllowedMountChange;
   public bool           Mounting;
   public NPC_Followable NPC;

   public bool AttemptMountChange(bool IsMounting)
   {
      if (Time.time < NextAllowedMountChange)
         return false;

      //Check if player is leading an NPC
      if (Player.ActiveState is PlayerNPCLocomotionState NPCLocoState)
         NPC = NPCLocoState.NPC;
      
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

      if (NPC != null)
         NPC.ActiveState = NPC.BoatState;
      
      Debug.Log("EnterBoat"); // Tom

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

      if (Boat.Instance.Dock.Boat_DockPoint != null)
      {
         //Reset Boat to the docked point, we do this so the boat is rotated 180 degrees so they can get out later
         Boat.Instance.transform.position = Boat.Instance.Dock.Boat_DockPoint.position;
         Boat.Instance.transform.rotation = Boat.Instance.Dock.Boat_DockPoint.rotation;
      }

      if (Boat.Instance.NPCInBoat != null && Boat.Instance.Dock.NPC_ExitPoint != null)
         Boat.Instance.NPCInBoat.ActiveState = Boat.Instance.NPCInBoat.IdleState;
      
        Debug.Log("ExitBoat"); // Tom
        

      AnimController.SetBool("Sitting", false);

      Player.Controller.enabled = true;
      Player.transform.SetParent(null);
      Player.transform.position = Boat.Instance.Dock.Player_ExitPoint.position;
      Player.transform.rotation = Boat.Instance.Dock.Player_ExitPoint.rotation; //TODO: This is unsafe, could put the player at a bad rotation
      //TODO: Zero off any Y rotation, so forward is a flat direction (X & Z)
   }

   public override void OnLeave()
   {
      base.OnLeave();
      NPC = null;
   }
}

//Player Driving Boat
public class PlayerBoatState : PlayerState
{
   public override bool CanInteract => true;
   public          Boat Boat        => Boat.Instance;

   public PlayerBoatState(Player Player) : base(Player) { }
   
   public override void OnUpdate()
   {
      if (InputManager.Boat_Brake.IsPressed) Boat.Brake();
      else
      {
         if (InputManager.Boat_LeftOar.IsPressed)  Boat.RotateOar(Boat.Left_Oar);
         if (InputManager.Boat_RightOar.IsPressed) Boat.RotateOar(Boat.Right_Oar);
      }
   }

   public override void OnAnimatorIK(int LayerIDx)
   {
      AnimController.SetIKPosition(AvatarIKGoal.LeftHand,  Boat.Left_Oar.IKPoint.position);
      AnimController.SetIKPosition(AvatarIKGoal.RightHand, Boat.Right_Oar.IKPoint.position);
      
      AnimController.SetIKPositionWeight(AvatarIKGoal.LeftHand, 1f);
      AnimController.SetIKPositionWeight(AvatarIKGoal.RightHand, 1f);
   }
}

public class PlayerNPCLocomotionState : PlayerLocomotionState
{
   public NPC_Followable NPC;
   
   public PlayerNPCLocomotionState(Player Character) : base(Character) {}
}

public class PlayerCarryNPCState : PlayerNPCLocomotionState
{
   public override bool CanInteract => AnimController.GetFloat("Speed") < 0.1f; //Check if we are standing still!
   public override bool CanJump     => false;
   
   public PlayerCarryNPCState(Player Character) : base(Character) {}

   public override void OnAnimatorIK(int LayerIDx)
   {
      base.OnAnimatorIK(LayerIDx);
      
      //Place Player Hands on the feet of the NPC being carried, to make it abit more believeable
      AnimController.SetIKPositionWeight(AvatarIKGoal.LeftHand, 1f);
      AnimController.SetIKPositionWeight(AvatarIKGoal.RightHand, 1f);
      
      AnimController.SetIKPosition(AvatarIKGoal.LeftHand,  Player.Carry_Foot_LeftIK.position  + (-Player.transform.right * 0.05f));
      AnimController.SetIKPosition(AvatarIKGoal.RightHand, Player.Carry_Foot_RightIK.position + (Player.transform.right * 0.05f));
   }
}

public class PlayerLeadingNPCState : PlayerNPCLocomotionState
{
   public bool           LeftSide = false;
   public Vector3        HandGoal;
   public AvatarIKGoal   IKGoal;

   public override bool CanInteract => AnimController.GetFloat("Speed") < 0.1f; //Check if we are standing still!
   public override bool CanJump     => false;
   
   public PlayerLeadingNPCState(Player Character) : base(Character) {}

   public override void OnUpdate()
   {
      base.OnUpdate();
      
      //TODO: Try controlling other NPC IK inside this IK, not sure if unity will like that or not. But it would make this cleaner
      IKGoal                    = LeftSide ? AvatarIKGoal.LeftHand  : AvatarIKGoal.RightHand;
      NPC.HoldHandsState.IKGoal = LeftSide ? AvatarIKGoal.RightHand : AvatarIKGoal.LeftHand;
      
      Vector3 NPCPosition    = NPC.transform.position;
      Vector3 PlayerPosition = Player.transform.position;
      Vector3 Direction      = Vector3.Normalize(PlayerPosition - NPCPosition);
      HandGoal = NPCPosition + (Vector3.up * NPC.HandHoldHeight) + (Direction * Vector3.Distance(NPCPosition, PlayerPosition) * 0.5f);
   }

   public override void OnAnimatorIK(int LayerIDx)
   {
      base.OnAnimatorIK(LayerIDx);
      
      AnimController.SetIKPositionWeight(IKGoal, 1f);
      AnimController.SetIKPosition(IKGoal, HandGoal);
   }
}

public class PlayerHoldingObjectState : PlayerLocomotionState
{
   public override bool  CanInteract => AnimController.GetFloat("Speed") < 0.1f; //Check if we are standing still!
   public override bool  CanJump     => false;
   public override float Speed       => base.Speed * Player.CarryingSpeedMod;

   public HoldableObject HoldableObject;
   
   public PlayerHoldingObjectState(Player Character) : base(Character) {}
   
   public override void OnUpdate()
   {
      base.OnUpdate();

      if (InputManager.Character_Interact.IsPressed)
      {
         //Try to place Object down
      }
   }

   public override void OnAnimatorIK(int LayerIDx)
   {
      base.OnAnimatorIK(LayerIDx);
      
      AnimController.SetIKPositionWeight(AvatarIKGoal.LeftHand, 1f);
      AnimController.SetIKPositionWeight(AvatarIKGoal.RightHand, 1f);
      
      AnimController.SetIKPosition(AvatarIKGoal.LeftHand, HoldableObject.LeftHand.position);
      AnimController.SetIKPosition(AvatarIKGoal.RightHand, HoldableObject.RightHand.position);
   }
}