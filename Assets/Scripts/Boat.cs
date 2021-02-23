﻿using Bitgem.VFX.StylisedWater;
using UnityEngine;

public class Boat : BaseBehaviour
{
   public static Boat Instance { get; private set; }
   
   public Rigidbody Rigid;
   public Transform PlayerSeat;
   
   public Vector3 P1;
   public Vector3 P2;
   public Vector3 P3;

   public Vector3 Left_Oar_Offset;
   public Vector3 Right_Oar_Offset;

   public float Max_Speed   = 5f;
   public float Brake_Force = 5f;
   public float Move_Force  = 5f;
   public float Oar_Delay   = 1.5f;

   public float Left_Oar_Delay;
   public float Right_Oar_Delay;
   public bool  IsBeingControlled = false;
   
   public override eUpdateFlags UpdateFlags => eUpdateFlags.RequireUpdate;

   public override void OnEnable()
   {
      base.OnEnable();
      Instance = this;
   }

   public override void OnUpdate(float DeltaTime)
   {
      WaterVolumeHelper WaterInstance = WaterVolumeHelper.Instance;
      if (WaterInstance == null)
         return;

      if (IsBeingControlled)
      {
         if (Left_Oar_Delay > 0)
            Left_Oar_Delay -= DeltaTime;

         if (Right_Oar_Delay > 0)
            Right_Oar_Delay -= DeltaTime;

         if (Left_Oar_Delay <= 0f && InputManager.Boat_LeftOar.IsPressed)
         {
            Rigid.AddForceAtPosition(transform.forward * Move_Force, transform.TransformPoint(Left_Oar_Offset), ForceMode.Force);
            Left_Oar_Delay = Oar_Delay;
         }

         if (Right_Oar_Delay <= 0f && InputManager.Boat_RightOar.IsPressed)
         {
            Rigid.AddForceAtPosition(transform.forward * Move_Force, transform.TransformPoint(Right_Oar_Offset), ForceMode.Force);
            Right_Oar_Delay = Oar_Delay;
         }

         if (InputManager.Boat_Brake.IsPressedOrHeld)
         {
            Rigid.velocity        -= Rigid.velocity * (Brake_Force * DeltaTime);
            Rigid.angularVelocity -= Rigid.angularVelocity * (Brake_Force * DeltaTime);
         }
         
         if(InputManager.Character_Mount.IsPressed)
            Unmount();
         
         Rigid.velocity        = Vector3.ClampMagnitude(Rigid.velocity, Max_Speed);
         Rigid.angularVelocity = Vector3.ClampMagnitude(Rigid.angularVelocity, Max_Speed);
      }

      Vector3   Forward  = transform.forward;
      Matrix4x4 LTW      = transform.localToWorldMatrix;
      Vector3   Position = LTW.MultiplyPoint3x4(Vector3.zero);
      Vector3   P1_W     = LTW.MultiplyPoint3x4(P1);
      Vector3   P2_W     = LTW.MultiplyPoint3x4(P2);
      Vector3   P3_W     = LTW.MultiplyPoint3x4(P3);

      P1_W.y     = WaterInstance.GetHeight(P1_W).GetValueOrDefault(P1_W.y);
      P2_W.y     = WaterInstance.GetHeight(P2_W).GetValueOrDefault(P2_W.y);
      P3_W.y     = WaterInstance.GetHeight(P3_W).GetValueOrDefault(P3_W.y);
      Position.y = (P1_W.y + P2_W.y + P3_W.y) / 3f;
      
      transform.rotation = Quaternion.LookRotation(Forward, Vector3.Cross(P2_W - P1_W, P3_W - P1_W).normalized);
      transform.position = Position;
   }

   public override void OnDisable()
   {
      base.OnDisable();
      Instance = null;
   }

   public void Mount()
   {
      if (IsBeingControlled || Player.Instance.MountDelay > 0)
         return;

      IsBeingControlled                  = true;
      Player.Instance.IsBeingControlled  = false;
      Player.Instance.Controller.enabled = false;
      Player.Instance.transform.SetParent(PlayerSeat);
      Player.Instance.transform.localPosition = Vector3.zero;
      Player.Instance.MountDelay              = 1f;
   }

   public void Unmount()
   {
      if (!IsBeingControlled || Player.Instance.MountDelay > 0)
         return;
      
      //TODO: Place player outside of the boat
      IsBeingControlled                  = false;
      Player.Instance.IsBeingControlled  = true;
      Player.Instance.Controller.enabled = true;
      Player.Instance.transform.SetParent(null);
      Player.Instance.MountDelay = 1f;
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
      
      
      Gizmos.DrawLine(Left_Oar_Offset, Left_Oar_Offset + Vector3.forward);
      Gizmos.DrawLine(Right_Oar_Offset, Right_Oar_Offset + Vector3.forward);
      
      Gizmos.DrawSphere(Left_Oar_Offset, 0.1f);
      Gizmos.DrawSphere(Right_Oar_Offset, 0.1f);
   }
}