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

public class DialogueContext
{
   public Character Target;
   public Character GetParticipant(string Name) => DialogueManager.Instance.FindParticipant(Name);
}

[Serializable]
public abstract class DialogueAction
{
   [HideInInspector]
   public string Participant;
   public abstract void Trigger(DialogueContext Context);
}

public class DialogueAnimationAction : DialogueAction
{
   public string StateName;

   public override void Trigger(DialogueContext Context)
   {
      Debug.Log($"Playing State: {StateName} On Character: {Context.Target.Name}");
      Context.Target.AnimController.Play(StateName);
   }
}

public class DialogueSetBoolAction : DialogueAction
{
   public string Key;
   public bool   Value;

   public override void Trigger(DialogueContext Context)
   {
      int IDx = Context.Target.GetDialogueBoolIDx(Key);
      if (IDx == -1)
      {
         Debug.LogError($"Failed to Find Bool: {Key} On {Context.Target.Name}");
         return;
      }

      //Update Bool on Participant
      Debug.Log($"Setting Dialogue Bool: {Key} = {Value} On Character: {Context.Target.Name}");
      Context.Target.Bools[IDx].Value = Value;
   }
}

public class DialogueGiveItemAction : DialogueAction
{
   public string ItemName;
   public string RecieverParticipant;
   
   public override void Trigger(DialogueContext Context)
   {
      int Item_IDx = Context.Target.GetInventoryItemIDx(ItemName);
      if (Item_IDx == -1)
      {
         Debug.LogError($"Participant: {Context.Target.name} Doesn't have item: {ItemName}");
         return;
      }

      Character Reciever = Context.GetParticipant(RecieverParticipant);
      if (Reciever == null)
      {
         Debug.LogError($"Failed to find Participant: {RecieverParticipant}");
         return;
      }

      Debug.Log($"{Participant} Is Giving Item: {ItemName} To: {RecieverParticipant}");
      Item TargetItem = Context.Target.RemoveItem(Item_IDx);
      
      if (TargetItem == null)
      {
         Debug.LogError($"Failed to Remove item: {ItemName} From: {RecieverParticipant}");
         return;
      }
      
      Reciever.AddItem(TargetItem);
   }
}

public class DialogueDisableParticipant : DialogueAction
{
   public override void Trigger(DialogueContext Context)
   {
      Debug.Log($"Disabling Partitpant: {Context.Target.name}");
      Context.Target.enabled = false;
   }
}