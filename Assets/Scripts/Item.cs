using UnityEngine;

public class Item : BaseBehaviour, IInteractable
{
   public string    ItemName;
   public Texture2D ItemIcon;
   public string    DialogueBool_Key;

   public int        InteractionPriority  => 1;
   public float      InteractionDistance  => 1f;
   public string     InteractionText      => $"Pick Up {ItemName}";
   public Vector3    Position             => transform.position;
   public Material[] InteractionMaterials { get; private set; }

   public override void OnEnable()
   {
      base.OnEnable();
      InteractionMaterials = this.CollectAllMaterials();
   }

   public void OnAddedToInventory(Character character)
   {
      if (string.IsNullOrEmpty(DialogueBool_Key))
         return;

      int Bool_IDx = character.GetDialogueBoolIDx(DialogueBool_Key);
      if (Bool_IDx == -1)
         return;

      character.Bools[Bool_IDx].Value = true;
   }

   public void OnRemoveFromInventory(Character character)
   {
      if (string.IsNullOrEmpty(DialogueBool_Key))
         return;
      
      int Bool_IDx = character.GetDialogueBoolIDx(DialogueBool_Key);
      if (Bool_IDx == -1)
         return;
      
      character.Bools[Bool_IDx].Value = false;
   }

   public bool CanInteract(Player player) => player.ActiveState is PlayerLocomotionState;

   public void OnInteract(Player player)
   {
      player.AddItem(this);
      gameObject.SetActive(false);
   }
}
