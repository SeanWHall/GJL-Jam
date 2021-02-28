using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DialogueManager : BaseBehaviour
{
    private static List<Character> _All_Characters = new List<Character>();
    
    public static DialogueManager Instance { get; private set; }
    public static bool IsInDialogue => Instance != null && Instance.DialogueAsset != null;
    
    public static void StartDialogue(DialogueAsset Asset)
    {
        if (IsInDialogue)
        {
            Instance.Error("Already in the middle of some dialogue, we can't start another one!");
            return;
        }

        if (Asset.Participants.Length == 0)
        {
            Instance.Error($"This dialogue has no participants {Asset.name}");
            return;
        }
        
        //Find all the participants that we need to start this dialogue
        int         Valid_Participants = 0;
        int         Participants_Len   = Asset.Participants.Length;
        Character[] Participants       = new Character[Participants_Len];
        int         Characters_Len     = GetBehaviours(_All_Characters);

        for (int i = 0; i < Participants_Len; i++)
        {
            string Name = Asset.Participants[i];
            for (int c = 0; c < Characters_Len; c++)
            {
                Character Current = _All_Characters[c];
                if(Current.Name != Name)
                    continue;

                Participants[i] = Current;
                break;
            }

            //Check if we found the correct character
            if (Participants[i] != null)
                continue;
            
            //If its still null, then we dont have everyone we need.
            Instance.Error($"Failed to find Participant: {Name} for dialogue: {Asset.name}");
            return;
        }
        
        //if we made it here, then we should be good to go
        Instance.Participants  = Participants;
        Instance.DialogueAsset = Asset;
        Instance.CurrentNode   = null;
        
        Instance.SendDialogueEvent(new DialogueEnterEvent());
        
        Instance.SetupNode(Asset.Nodes[0]);
        HUD.Instance.ShowDialogue();
    }

    public Character[]   Participants;
    public DialogueAsset DialogueAsset;
    public DialogueNode  CurrentNode;
    
    public override void OnEnable()
    {
        base.OnEnable();

        Instance = this;
    }

    private void SendDialogueEvent(DialogueEvent Event)
    {
        if (Event == null)
            return;
        
        foreach(var Participant in Participants)
            Participant.OnDialogueEvent(Event);
    }

    public void SetupNode(DialogueNode Node)
    {
        HUD.Instance.ClearDialogueOptions();

        HUD.Instance.Dialogue_Text.text = Node.Dialogue;

        int Actions_Len = Node.Actions.Length;
        for (int i = 0; i < Actions_Len; i++)
        {
            DialogueAction Action = Node.Actions[i].Action;
            if(Action == null)
                continue;

            Character Participant = FindParticipant(Action.Participant);
            if (Participant == null)
            {
                Error($"Failed to find Participant: {Action.Participant} For Action: {Action.GetType().Name}");
                continue;
            }
            
            //Run Action in try 'n Catch, so as to not impede the Dialogue if an exception is thrown
            try { Action.Trigger(Participant);}
            catch (Exception Ex) { Debug.LogException(Ex); }
        }
        
        int Options_Len = Node.Options.Length;
        for (int i = 0; i < Options_Len; i++)
        {
            DialogueOption Option = Node.Options[i];
            HUD.Instance.AddDialogueOption(Option.Text, OnOptionClick);

            void OnOptionClick()
            {
                int Node_IDx = DialogueAsset.GetNodeIDx(Option.NextNode);
                if (Node_IDx == -1)
                {
                    Error($"Failed to find node: {Option.NextNode}, exiting dialogue");
                    CloseDialogue();
                }
                
                SetupNode(DialogueAsset.Nodes[Node_IDx]);
            }
        }
        
        if(Node.AllowLeave)
            HUD.Instance.AddDialogueOption("Leave", CloseDialogue);
    }

    public void CloseDialogue()
    {
        HUD.Instance.HideDialogue();

        SendDialogueEvent(new DialogueLeaveEvent());
        
        Participants  = null;
        DialogueAsset = null;
        CurrentNode   = null;
    }

    public Character FindParticipant(string Name)
    {
        int Len = Participants.Length;
        for (int i = 0; i < Len; i++)
        {
            if (Participants[i].Name == Name)
                return Participants[i];
        }

        return null;
    }
}

//TODO: Use for some conditional checks, if we need to... Other wise this can be simplified down at some point
public abstract class DialogueEvent {}
public class DialogueEnterEvent : DialogueEvent {}
public class DialogueLeaveEvent : DialogueEvent {}