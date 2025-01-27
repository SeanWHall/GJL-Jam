﻿using UnityEngine;
using UnityEngine.InputSystem;

public class NPC_Followable : NPC
{
    public float   HandHoldHeight  = 0.5f;
    public float   DesiredDistance = 0.5f; //Distance to try and get to from the Player
    public float   BreakDistance   = 2f; //Distance before NPC stops following
    public Vector3 HandOffset      = Vector3.zero;
    
    public NPCCarryState     CarryState;
    public NPCHoldHandsState HoldHandsState;
    public NPCBoatState      BoatState;
    
    public override void OnEnable()
    {
        base.OnEnable();

        CarryState     = new NPCCarryState(this);
        HoldHandsState = new NPCHoldHandsState(this);
        BoatState      = new NPCBoatState(this);
        
        if(GetDialogueBoolIDx("Follow") == -1)
            Warn("A Followable NPC, doesn't have a follow bool setup!");
    }

    public override void OnLeaveDialogue(Character[] Participants)
    {
        if (!enabled)
            return;
        
        //Check if the NPC is currently following
        int Follow_IDx = GetDialogueBoolIDx("Follow");
        if (Follow_IDx == -1)
        {
            ActiveState = IdleState;
            return;
        }

        int Carry_IDx = GetDialogueBoolIDx("Carry");
        if (Carry_IDx != -1 && Bools[Carry_IDx].Value)
        {
            ActiveState = CarryState;
            return;
        }
        
        ActiveState = Bools[Follow_IDx].Value ? (NPCState)HoldHandsState : IdleState;
    }
}

public class NPCBoatState : NPCState
{
    public NPCBoatState(NPC NPC) : base(NPC) {}

    public override void OnEnter()
    {
        base.OnEnter();

        Agent.enabled = false;

        NPC.transform.parent        = Boat.Instance.NPCSeat;
        NPC.transform.localPosition = Vector3.zero;
        NPC.transform.localRotation = Quaternion.identity;
        
        Boat.Instance.NPCInBoat = NPC;
        AnimController.SetBool("IsSitting", true);
    }

    public override void OnLeave()
    {
        base.OnLeave();

        //Unparent the NPC from the dock
        NPC.transform.parent   = null;
        NPC.transform.position = Boat.Instance.Dock.NPC_ExitPoint.position;
        
        AnimController.SetBool("IsSitting", false);
        Agent.enabled = true;
    }
}

public class NPCFollowingState : NPCState
{
    public Player                Player     => Player.Instance;
    public PlayerCarryNPCState   CarryState => Player.Instance.CarryNPCState;
    public PlayerLeadingNPCState LeadState  => Player.Instance.LeadingNPCState;
    
    public NPCFollowingState(Character Character) : base(Character) {}
}

public class NPCCarryState : NPCFollowingState
{
    public NPCCarryState(Character Character) : base(Character) {}

    public override void OnEnter()
    {
        base.OnEnter();

        //Disable the agent, since we are being carried
        Agent.enabled = false;
        
        //Mount the NPC to the back of the player
        NPC.transform.parent = Player.Instance.Carry_MountPoint;
        NPC.transform.localPosition = Vector3.zero;
        NPC.transform.localRotation = Quaternion.identity;
        
        AnimController.SetBool("IsCarried", true);
    }

    public override void OnAnimatorIK(int LayerIDx)
    {
        base.OnAnimatorIK(LayerIDx);
        
        AnimController.SetIKPositionWeight(AvatarIKGoal.LeftHand,  1f);
        AnimController.SetIKPositionWeight(AvatarIKGoal.RightHand, 1f);
        
        AnimController.SetIKPosition(AvatarIKGoal.LeftHand, Player.Carry_Hand_LeftIK.position);
        AnimController.SetIKPosition(AvatarIKGoal.RightHand, Player.Carry_Hand_RightIK.position);
        
        AnimController.SetIKPositionWeight(AvatarIKGoal.LeftFoot,  1f);
        AnimController.SetIKPositionWeight(AvatarIKGoal.RightFoot, 1f);
        
        AnimController.SetIKPosition(AvatarIKGoal.LeftFoot, Player.Carry_Foot_LeftIK.position   + new Vector3(0, 0.1f));
        AnimController.SetIKPosition(AvatarIKGoal.RightFoot, Player.Carry_Foot_RightIK.position + new Vector3(0, 0.1f));
    }

    public override void OnLeave()
    {
        base.OnLeave();

        AnimController.SetBool("IsCarried", false);
        
        //Unparent NPC
        NPC.transform.parent = null;
        
        //Place npc at same Y Level of the player
        Vector3 NPC_Pos        = NPC.transform.position;
        NPC_Pos.y              = Player.Instance.transform.position.y;
        NPC.transform.position = NPC_Pos;

        //Renable the agent
        Agent.enabled            = true;
    }
}

public class NPCHoldHandsState : NPCFollowingState
{
    public NPCHoldHandsState(NPC NPC) : base(NPC) {}

    public AvatarIKGoal IKGoal;
    
    public override void OnUpdate()
    {
        base.OnUpdate();
        
        Vector3 NPCPosition    = NPC.transform.position;
        Vector3 PlayerPosition = Player.transform.position;
        
        if (Vector3.Distance(NPCPosition, PlayerPosition) > ((NPC_Followable)NPC).BreakDistance)
        {
            NPC.ActiveState = NPC.IdleState;
            return;
        }

        //TODO: Might need to do some physics checks, to alter the following goal so it remains on the nav mesh
        //TODO: Should ready cache the transform and rotation, instead of constantly accessing it. Unity is stupid about this
        //This is horrible, but hopefully this will sell the effect
        Vector3 Player_LeftSide  = PlayerPosition + (-Player.transform.right * (Player.NavObstacle.radius + ((NPC_Followable)NPC).DesiredDistance));
        Vector3 Player_RightSide = PlayerPosition + (Player.transform.right * (Player.NavObstacle.radius + ((NPC_Followable)NPC).DesiredDistance));

        LeadState.LeftSide = Vector3.Distance(NPCPosition, Player_LeftSide) < Vector3.Distance(NPCPosition, Player_RightSide);
        Agent.SetDestination(LeadState.LeftSide ? Player_LeftSide : Player_RightSide);
        
        AnimController.SetFloat("Speed", Agent.velocity.magnitude);
    }

    public override void OnAnimatorIK(int LayerIDx)
    {
        base.OnAnimatorIK(LayerIDx);
        
        AnimController.SetIKPositionWeight(IKGoal, 1f);
        AnimController.SetIKPosition(IKGoal, LeadState.HandGoal + ((NPC_Followable)NPC).HandOffset);
    }

    public override void OnLeave()
    {
        base.OnLeave();
        AnimController.SetFloat("Speed", 0);
    }
}