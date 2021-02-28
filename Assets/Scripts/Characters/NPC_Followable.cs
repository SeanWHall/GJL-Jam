﻿using UnityEngine;
using UnityEngine.InputSystem;

public class NPC_Followable : NPC
{
    public float   HandHoldHeight  = 0.5f;
    public float   DesiredDistance = 0.5f; //Distance to try and get to from the Player
    public float   BreakDistance   = 2f; //Distance before NPC stops following
    public Vector3 HandOffset      = Vector3.zero;
    
    public NPCCarryState     CarryState;
    public NPCFollowingState FollowingState;
    public NPCBoatState      BoatState;
    
    public override void OnEnable()
    {
        base.OnEnable();

        CarryState     = new NPCCarryState(this);
        FollowingState = new NPCFollowingState(this);
        BoatState      = new NPCBoatState(this);
        
        if(GetDialogueBoolIDx("Follow") == -1)
            Warn("A Followable NPC, doesn't have a follow bool setup!");
    }

    public override void OnLeaveDialogue()
    {
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
        ActiveState = Bools[Follow_IDx].Value ? (NPCState)FollowingState : IdleState;
    }
}

public class NPCBoatState : NPCState
{
    public NPCBoatState(NPC NPC) : base(NPC) {}
}

public class NPCCarryState : NPCState
{
    public NPCCarryState(Character Character) : base(Character) {}
}

public class NPCFollowingState : NPCState
{
    public PlayerLeadingNPCState PlayerState => Player.Instance.LeadingNPCState;
    
    public NPCFollowingState(NPC NPC) : base(NPC) {}

    public AvatarIKGoal IKGoal;
    
    public override void OnEnter()
    {
        //TODO: Finish Ai Logic, Player needs to be the one updating it's values not the NPC
        //Player.Following = NPC;
    }

    public override void OnUpdate()
    {
        Vector3 NPCPosition    = NPC.transform.position;
        Vector3 PlayerPosition = Player.transform.position;
        
        if (Vector3.Distance(NPCPosition, PlayerPosition) > ((NPC_Followable)NPC).BreakDistance || Keyboard.current.lKey.isPressed)
        {
            NPC.ActiveState = NPC.IdleState;
            return;
        }
            

        //TODO: Might need to do some physics checks, to alter the following goal so it remains on the nav mesh
        //TODO: Should ready cache the transform and rotation, instead of constantly accessing it. Unity is stupid about this
        //This is horrible, but hopefully this will sell the effect
        Vector3 Player_LeftSide  = PlayerPosition + (-Player.transform.right * (Player.NavObstacle.radius + ((NPC_Followable)NPC).DesiredDistance));
        Vector3 Player_RightSide = PlayerPosition + (Player.transform.right * (Player.NavObstacle.radius + ((NPC_Followable)NPC).DesiredDistance));

        PlayerState.LeftSide = Vector3.Distance(NPCPosition, Player_LeftSide) < Vector3.Distance(NPCPosition, Player_RightSide);
        Agent.SetDestination(PlayerState.LeftSide ? Player_LeftSide : Player_RightSide);
        
        AnimController.SetFloat("Speed", Agent.velocity.magnitude);
    }

    public override void OnAnimatorIK(int LayerIDx)
    {
        base.OnAnimatorIK(LayerIDx);
        
        AnimController.SetIKPositionWeight(IKGoal, 1f);
        AnimController.SetIKPosition(IKGoal, PlayerState.HandGoal + ((NPC_Followable)NPC).HandOffset);
    }

    public override void OnLeave()
    {
        base.OnLeave();
        //TODO: Player should be the one updating it's values
        //Player.Following = null;
    }
}