using System;
using UnityEngine;
using UnityEngine.Serialization;

public class DockArea : BaseBehaviour
{
   [FormerlySerializedAs("ExitPoint")]
   public Transform Player_ExitPoint;
   public Transform NPC_ExitPoint;
   public Transform Boat_DockPoint;

   public float DockSize { get; private set; }

   public override void OnEnable()
   {
      base.OnEnable();

      Collider TriggerCol = GetComponent<Collider>();
      DockSize = TriggerCol != null ?TriggerCol.bounds.size.magnitude : float.PositiveInfinity;
   }

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
