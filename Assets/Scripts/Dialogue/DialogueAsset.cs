using System;
using UnityEngine;

//Very Rough Dialogue Asset
[CreateAssetMenu]
public class DialogueAsset : ScriptableObject
{
   public string[]       Participants; //used to validate against
   public DialogueNode[] Nodes;

   public int GetNodeIDx(string Name)
   {
      int Len = Nodes.Length;
      for (int i = 0; i < Len; i++)
      {
         if (Nodes[i].Name == Name)
            return i;
      }

      return -1;
   }
   
   public bool HasParticipant(string Name)
   {
      int Len = Participants.Length;
      for (int i = 0; i < Len; i++)
      {
         if (Participants[i] == Name)
            return true;
      }

      return false;
   }
}

[Serializable]
public class DialogueNode
{
   public string             Name; //Used to find Node
   public string             Dialogue;
   public bool               AllowLeave = true;
   public DialogueOption[]   Options = new DialogueOption[0];
   public SerializedAction[] Actions = new SerializedAction[0];
}

[Serializable]
public class DialogueOption
{
   public string             Text;
   public string             NextNode;
   public ParticipantBools[] Bools = new ParticipantBools[0]; //Requires these bools to be shown
}

[Serializable]
public struct ParticipantBools
{
   public string Participant;
   public string Key;
   public bool   State;
}

[Serializable]
public class SerializedAction
{
   public string Type;
   public string JSon;

   [NonSerialized]
   private DialogueAction _Action;
   public DialogueAction Action => _Action ?? (_Action = (DialogueAction)JsonUtility.FromJson(JSon, System.Type.GetType(Type)));
}

[Serializable]
public abstract class DialogueAction
{
   [HideInInspector]
   public string Participant;
   public abstract void Trigger(Character Context);
}

public class DialogueAnimationAction : DialogueAction
{
   public string StateName;

   public override void Trigger(Character Context)
   {
      Debug.Log($"Playing State: {StateName} On Character: {Context.Name}");
      Context.AnimController.Play(StateName);
   }
}

public class DialogueSetBoolAction : DialogueAction
{
   public string Key;
   public bool   Value;

   public override void Trigger(Character Context)
   {
      Debug.Log($"Setting Dialogue Bool: {Key} = {Value} On Character: {Context.Name}");
      
      int IDx = Context.GetDialogueBoolIDx(Key);
      if (IDx == -1)
         return;

      //Update Bool on Participant
      Context.Bools[IDx].Value = Value;
   }
}