using UnityEngine;

public class Item : BaseBehaviour, IInteractable
{
   public string    ItemName;
   public Texture2D ItemIcon;
   public Item      ItemPrefab;

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

   public bool CanInteract(Player player) => player.ActiveState is PlayerLocomotionState;

   public void OnInteract(Player player)
   {
      player.Inventory.Add(ItemPrefab);
      Destroy(gameObject); //Destroy instance of the prefab
   }
}
