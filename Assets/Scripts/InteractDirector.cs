using UnityEngine;
using UnityEngine.Playables;

public class InteractDirector : BaseBehaviour, IInteractable
{
   public string           Interact_Text;
   public float            Interact_Distance = 2;
   public PlayableDirector Target;
   
   public float      InteractionDistance  => Interact_Distance;
   public string     InteractionText      => Interact_Text;
   public Vector3    Position             => transform.position;
   public Material[] InteractionMaterials => null;

   private bool m_Triggered;

   public bool CanInteract(Player player) => !m_Triggered && Target != null;

   public void       OnInteract(Player player)
   {
      if (m_Triggered)
         return;
      
      m_Triggered = true;
      Target.Play();
   }
}
