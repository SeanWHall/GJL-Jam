using System.Collections;
using Bitgem.VFX.StylisedWater;
using UnityEngine;

[System.Serializable]
public class BoatOar
{
   public Transform ForceOffset;
   public Transform IKPoint;
   public Transform MeshTarget;
   public Vector3   Rotation;

   public Quaternion Local_StartRot;
   public Coroutine  Rotate_Routine;
}

public class Boat : BaseBehaviour
{
   public static Boat Instance { get; private set; }
   
   public Rigidbody Rigid;
   public Transform PlayerSeat;
   public float     WaterOffset = 0f;
   
   public Vector3 P1;
   public Vector3 P2;
   public Vector3 P3;

   public BoatOar Left_Oar;
   public BoatOar Right_Oar;

   public float Oar_RotateSpeed = 2f;
   public float Max_Speed       = 5f;
   public float Brake_Force     = 5f;
   public float Move_Force      = 5f;

   public DockArea Dock;
   
   public override eUpdateFlags UpdateFlags => eUpdateFlags.RequireUpdate;

   public override void OnEnable()
   {
      base.OnEnable();
      Instance = this;

      Left_Oar.Local_StartRot  = Left_Oar.MeshTarget.localRotation;
      Right_Oar.Local_StartRot = Right_Oar.MeshTarget.localRotation;
   }

   public bool RotateOar(BoatOar Oar)
   {
      if (Oar.Rotate_Routine != null)
         return false;
      
      Rigid.AddForceAtPosition(transform.forward * Move_Force, Oar.ForceOffset.position, ForceMode.Force);
      Oar.Rotate_Routine = StartCoroutine(RoateOarAsync(Oar));
      return true;
   }

   public void Brake()
   {
      Rigid.velocity        -= Rigid.velocity * (Brake_Force * Time.deltaTime);
      Rigid.angularVelocity -= Rigid.angularVelocity * (Brake_Force * Time.deltaTime);
   }

   //This is not good practice, AKA STAY AWAY FROM COROUTINES. But should be fine for this jam
   private IEnumerator RoateOarAsync(BoatOar TargetOar)
   {
      float Alpha = 0f;
      while (Alpha < 1f)
      {
         TargetOar.MeshTarget.localRotation = TargetOar.Local_StartRot * Quaternion.Euler(TargetOar.Rotation * (360 * Alpha));
         yield return new WaitForEndOfFrame();
         Alpha += Oar_RotateSpeed * Time.deltaTime;
      }

      TargetOar.Rotate_Routine = null;
   }
   
   public override void OnUpdate(float DeltaTime)
   {
      WaterVolumeHelper WaterInstance = WaterVolumeHelper.Instance;
      if (WaterInstance == null)
         return;

      Rigid.velocity        = Vector3.ClampMagnitude(Rigid.velocity, Max_Speed);
      Rigid.angularVelocity = Vector3.ClampMagnitude(Rigid.angularVelocity, Max_Speed);

      Vector3   Forward  = transform.forward;
      Matrix4x4 LTW      = transform.localToWorldMatrix;
      Vector3   Position = LTW.MultiplyPoint3x4(Vector3.zero);
      Vector3   P1_W     = LTW.MultiplyPoint3x4(P1);
      Vector3   P2_W     = LTW.MultiplyPoint3x4(P2);
      Vector3   P3_W     = LTW.MultiplyPoint3x4(P3);

      P1_W.y     = WaterInstance.GetHeight(P1_W).GetValueOrDefault(P1_W.y);
      P2_W.y     = WaterInstance.GetHeight(P2_W).GetValueOrDefault(P2_W.y);
      P3_W.y     = WaterInstance.GetHeight(P3_W).GetValueOrDefault(P3_W.y);
      Position.y = ((P1_W.y + P2_W.y + P3_W.y) / 3f) + WaterOffset;
      
      Debug.DrawLine(LTW.MultiplyPoint3x4(P1), P1_W, Color.magenta);
      Debug.DrawLine(LTW.MultiplyPoint3x4(P2), P2_W, Color.magenta);
      Debug.DrawLine(LTW.MultiplyPoint3x4(P3), P3_W, Color.magenta);
      
      transform.rotation = Quaternion.LookRotation(Forward, Vector3.Cross(P2_W - P1_W, P3_W - P1_W).normalized);
      transform.position = Position;
   }

   public override void OnDisable()
   {
      base.OnDisable();
      Instance = null;
   }
   
   private void OnDrawGizmos()
   {
      Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);
      
      Gizmos.DrawLine(P1, P2);
      Gizmos.DrawLine(P2, P3);
      Gizmos.DrawLine(P3, P1);
      
      Gizmos.DrawSphere(P1, 0.1f);
      Gizmos.DrawSphere(P2, 0.1f);
      Gizmos.DrawSphere(P3, 0.1f);

      Vector3 Center = (P1 + P2 + P3) / 3f;
      Gizmos.DrawLine(Center, Center + (Vector3.Cross(P2 - P1, P3 - P1).normalized));
      Gizmos.DrawSphere(Center, 0.1f);
   }
}
