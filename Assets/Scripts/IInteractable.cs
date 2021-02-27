using UnityEngine;

public interface IInteractable
{
   float      InteractionDistance  { get;} //Distance before CanInteract will be called
   string     InteractionText      { get; } //Shown on the UI
   Vector3    Position             { get; }
   Material[] InteractionMaterials { get; } //Materials to enable rim on
   
   bool CanInteract(Player player); //While true, interaction will be shown on screen
   void OnInteract(Player player);
}
