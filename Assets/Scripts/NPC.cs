﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;

public class NPC : Character
{
    public float   HandHoldHeight  = 0.5f;
    public float   DesiredDistance = 0.5f; //Distance to try and get to from the Player
    public float   BreakDistance   = 2f; //Distance before NPC stops following
    public Vector3 HandOffset = Vector3.zero;

    [HideInInspector] public Animator     AnimController;
    [HideInInspector] public NavMeshAgent Agent;

    public NPCIdleState      IdleState;
    public NPCBoatState      BoatState;
    public NPCFollowingState FollowingState;
    
    public override void OnEnable()
    {
        base.OnEnable();

        AnimController = GetComponent<Animator>();
        Agent          = GetComponent<NavMeshAgent>();

        IdleState      = new NPCIdleState(this);
        BoatState      = new NPCBoatState(this);
        FollowingState = new NPCFollowingState(this);

        ActiveState = IdleState;
    }
}

public class NPCState : CharacterState
{
    public NPC          NPC;
    public NavMeshAgent Agent          => NPC.Agent;
    public Animator     AnimController => NPC.AnimController;
    public Player       Player         => Player.Instance;

    public NPCState(NPC NPC) => this.NPC = NPC;
}

public class NPCIdleState : NPCState
{
    public NPCIdleState(NPC NPC) : base(NPC) {}

    public override void OnUpdate()
    {
        base.OnUpdate();

        if (Keyboard.current.jKey.isPressed && Player.Following == null)
        {
            //Only Switch to the following state, if no other NPC is currently following the player
            NPC.ActiveState = NPC.FollowingState;
        }
    }
}

public class NPCBoatState : NPCState
{
    public NPCBoatState(NPC NPC) : base(NPC) {}
}

public class NPCFollowingState : NPCState
{
    public NPCFollowingState(NPC NPC) : base(NPC) {}

    public AvatarIKGoal IKGoal;
    
    public override void OnEnter()
    {
        Player.Following = NPC;
    }

    public override void OnUpdate()
    {
        Vector3 NPCPosition    = NPC.transform.position;
        Vector3 PlayerPosition = Player.transform.position;
        
        if (Vector3.Distance(NPCPosition, PlayerPosition) > NPC.BreakDistance || Keyboard.current.lKey.isPressed)
        {
            NPC.ActiveState = NPC.IdleState;
            return;
        }
            

        //TODO: Might need to do some physics checks, to alter the following goal so it remains on the nav mesh
        //TODO: Should ready cache the transform and rotation, instead of constantly accessing it. Unity is stupid about this
        //This is horrible, but hopefully this will sell the effect
        Vector3 Player_LeftSide  = PlayerPosition + (-Player.transform.right * (Player.NavObstacle.radius + NPC.DesiredDistance));
        Vector3 Player_RightSide = PlayerPosition + (Player.transform.right * (Player.NavObstacle.radius + NPC.DesiredDistance));

        Player.LeftSide = Vector3.Distance(NPCPosition, Player_LeftSide) < Vector3.Distance(NPCPosition, Player_RightSide);
        Agent.SetDestination(Player.LeftSide ? Player_LeftSide : Player_RightSide);
        
        AnimController.SetFloat("Speed", Agent.velocity.magnitude);
        Debug.Log($"Is left Side?: {Player.LeftSide}");
    }

    public override void OnAnimatorIK(int LayerIDx)
    {
        base.OnAnimatorIK(LayerIDx);
        
        AnimController.SetIKPositionWeight(IKGoal, 1f);
        AnimController.SetIKPosition(IKGoal, Player.HandGoal + NPC.HandOffset);
    }

    public override void OnLeave()
    {
        base.OnLeave();
        Player.Following = null;
    }
}