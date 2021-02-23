﻿using UnityEngine;

public class CameraController : BaseBehaviour
{
   public static CameraController Instance { get; private set; }
   
   public Camera Camera;
   
   public override eUpdateFlags UpdateFlags => eUpdateFlags.RequireUpdate;

   private CameraState _ActiveState;
   public CameraState ActiveState
   {
      get => _ActiveState;
      set
      {
         if (_ActiveState == value)
            return;
         
         _ActiveState?.OnExit();
         _ActiveState = value;
         _ActiveState?.OnEnter();
      }
   }
   
   public PlayerCameraState PlayerState;
   public BoatCameraState   BoatState;

   public override void OnEnable()
   {
      base.OnEnable();
      
      PlayerState = new PlayerCameraState(this);
      BoatState   = new BoatCameraState(this);

      ActiveState = PlayerState;
      Instance    = this;
   }

   public override void OnUpdate(float DeltaTime) => ActiveState?.OnUpdate();

   public override void OnDisable()
   {
      base.OnDisable();
      Instance = null;
   }
}

[System.Serializable]
public struct CamParams
{
   public static CamParams Default => new CamParams
   {
      Direction = Vector3.forward,
      FOV       = 45,
      Distance  = 10,
      Speed     = 5
   };
   
   public Vector3 Direction;
   public float   FOV;
   public float   Distance;
   public float   Speed;

   public CamParams Lerp(CamParams Target, float Alpha)
   {
      return new CamParams
      {
         Direction = Vector3.Lerp(Direction, Target.Direction, Alpha),
         FOV       = Mathf.Lerp(FOV,         Target.FOV,       Alpha),
         Distance  = Mathf.Lerp(Distance,    Target.Distance,  Alpha),
         Speed     = Mathf.Lerp(Speed,       Target.Speed,     Alpha),
      };
   }
   
   public Vector3 CalculatePosition(Vector3 Center) => Center + -Direction * Distance;
}

public class CameraState
{
   public CameraController Controller;

   public Camera Camera => Controller.Camera;
   
   public CameraState(CameraController Controller) => this.Controller = Controller;
   
   public virtual void OnEnter()  {}
   public virtual void OnUpdate() {}
   public virtual void OnExit()   {}
}

public class PlayerCameraState : CameraState
{
   public CamParams Current          = CamParams.Default;
   public CamParams Target           = CamParams.Default;
   public float     Alpha            = 1f;
   public float     InterpolateSpeed = 1f;
   
   public PlayerCameraState(CameraController Controller) : base(Controller) {}
   
   public override void OnUpdate()
   {
      if (Alpha < 1f)
      {
         //Lerp to Target
         Current =  Current.Lerp(Target, Alpha);
         Alpha   += InterpolateSpeed * Time.deltaTime;
      }

      //Calculate Camera offset based on Params direction & distance
      Vector3 Current_Camera_Pos = Camera.transform.position;
      Vector3 Next_Camera_Pos    = Current.CalculatePosition(Player.Instance.transform.position);
      
      Camera.fieldOfView        = Current.FOV;
      Camera.transform.forward  = Current.Direction;
      Camera.transform.position = Vector3.Lerp(Current_Camera_Pos, Next_Camera_Pos, Current.Speed * Time.deltaTime);
   }

   public void LerpTo(CamParams Params, float Speed)
   {
      Target           = Params;
      InterpolateSpeed = Speed;
      Alpha            = 0f;
   }
}

public class BoatCameraState : CameraState
{
   public BoatCameraState(CameraController Controller) : base(Controller) {}
   
   public override void OnUpdate()
   {
      
   }
}