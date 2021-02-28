using System.Collections.Generic;
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

public static class IInteractableExtensions
{
   private static List<MeshRenderer> _All_Renderers = new List<MeshRenderer>();
   private static List<Material>     _All_Materials = new List<Material>();
   
   public static Material[] CollectAllMaterials(this IInteractable Interactable)
   {
      if (!(Interactable is Component Comp))
         return null;

      Comp.GetComponentsInChildren(_All_Renderers);
      int            Renderers_Len = _All_Renderers.Count;
        
      for(int i = 0; i < Renderers_Len; i++)
         _All_Materials.AddRange(_All_Renderers[i].materials);

      Material[] Temp = _All_Materials.ToArray();
      _All_Materials.Clear();
      _All_Renderers.Clear();
      return Temp;
   }
}