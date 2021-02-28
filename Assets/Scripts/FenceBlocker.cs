using UnityEngine;

public class FenceBlocker : BaseBehaviour, IInteractable
{
   public int        InteractionPriority  => 7;
   public float      InteractionDistance  => 5f;
   public string     InteractionText      => "Cut Fence";
   public Vector3    Position             => transform.position;
   public Material[] InteractionMaterials { get; private set; }

   public override void OnEnable()
   {
      base.OnEnable();
      InteractionMaterials = this.CollectAllMaterials();
   }
   
   public bool CanInteract(Player player) => player.GetInventoryItemIDx("Cutters") != -1;
   public void OnInteract(Player player) => gameObject.SetActive(false);
}
