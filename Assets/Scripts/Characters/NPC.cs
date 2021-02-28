using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;

[System.Serializable]
public class DialogueReference
{
    public string        Key;
    public DialogueAsset Asset;
}

public class NPC : Character, IInteractable
{
    public float   CommunicationDistance = 2f;
    

    public DialogueReference[] Dialogues;
    
    [HideInInspector] public NavMeshAgent Agent;

    public NPCIdleState      IdleState;
    public NPCBoatState      BoatState;
    public NPCFollowingState FollowingState;
    public NPCDialogueState  DialogueState;

    public float      InteractionDistance  => CommunicationDistance;
    public string     InteractionText      => $"Communicate With {Name}";
    public Vector3    Position             => transform.position;
    public Material[] InteractionMaterials { get; private set; }
    
    public override void OnEnable()
    {
        base.OnEnable();

        AnimController = GetComponent<Animator>();
        Agent          = GetComponent<NavMeshAgent>();

        IdleState      = new NPCIdleState(this);
        BoatState      = new NPCBoatState(this);
        FollowingState = new NPCFollowingState(this);
        DialogueState  = new NPCDialogueState(this);

        ActiveState = IdleState;

        InteractionMaterials = this.CollectAllMaterials();
    }
    
    public override void OnEnterDialogue() => ActiveState = DialogueState;
    public override void OnLeaveDialogue() => ActiveState = IdleState; //TODO: Check if NPC is being Lead or carried

    public DialogueAsset GetActiveDialogue()
    {
        int Dialogues_Len = Dialogues.Length;
        for (int i = 0; i < Dialogues_Len; i++)
        {
            DialogueReference Reference = Dialogues[i];
            int               BoolIDx   = GetDialogueBoolIDx(Reference.Key);
            
            if(BoolIDx == -1 || !Bools[BoolIDx].Value)
                continue;

            return Reference.Asset;
        }

        return null;
    }

    public bool CanInteract(Player player) => player.ActiveState == player.LocomotionState && GetActiveDialogue() != null;
    public void OnInteract(Player player)  => DialogueManager.StartDialogue(GetActiveDialogue());
}

public class NPCState : CharacterState
{
    public NPC          NPC    => (NPC) Character;
    public NavMeshAgent Agent  => NPC.Agent;
    public Player       Player => Player.Instance;

    public NPCState(Character Character) : base(Character) {}
}

public class NPCIdleState : NPCState
{
    public NPCIdleState(NPC NPC) : base(NPC) {}

    public override void OnUpdate()
    {
        base.OnUpdate();

        //TODO: Finish AI Logic
        //if (Keyboard.current.jKey.isPressed && Player.Following == null)
        //{
        //    //Only Switch to the following state, if no other NPC is currently following the player
        //    NPC.ActiveState = NPC.FollowingState;
        //}
    }
}

public class NPCDialogueState : NPCState
{
    public NPCDialogueState(NPC NPC) : base(NPC) {}
    
    
    public override void OnEnter()
    {
        base.OnEnter();
      
        //TODO: Face towards the Participants of the dialogue
    }
}