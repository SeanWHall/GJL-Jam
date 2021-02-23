using System;
using UnityEngine;

public class DockArea : BaseBehaviour
{
   public Transform ExitPoint;
   
   public void OnTriggerEnter(Collider other)
   {
      if (!IsColliderPartOfBoat(other))
         return;

      Boat.Instance.Dock = this;
   }

   public void OnTriggerExit(Collider other)
   {
      if (!IsColliderPartOfBoat(other))
         return;
      
      if(Boat.Instance.Dock == this)
         Boat.Instance.Dock = null;
   }

   private bool IsColliderPartOfBoat(Collider Col) => Col.GetComponentInChildren<Boat>() || Col.GetComponentInParent<Boat>() != null;

   public void OnDrawGizmos()
   {
      Gizmos.color  = Color.white;
      Gizmos.matrix = transform.localToWorldMatrix;
      Gizmos.DrawWireCube(Vector3.zero, Vector3.one);
      
      Gizmos.color = Boat.Instance != null && Boat.Instance.Dock == this ? new Color(0, 1f, 0, 0.5f) : new Color(1f, 0, 0, 0.5f);
      Gizmos.DrawCube(Vector3.zero, Vector3.one);
   }
}
