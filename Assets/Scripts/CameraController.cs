using UnityEngine;

public class CameraController : BaseBehaviour
{
   public static CameraController Instance         { get; private set; }
   public static float            WorldOrientation => Instance != null ? Instance.m_WorldOrientation : 0;
   
   public Camera  Camera;
   public float   CameaLength     = 5f;
   public float   LookSensitivity = 0.5f;
   public Vector2 lookXLimit      = new Vector2(-180, 180f);
   public Vector2 lookYLimit      = new Vector2(30.0f, 90f);
   
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
   
   private float m_WorldOrientation;

   public override void OnEnable()
   {
      base.OnEnable();
      
      PlayerState = new PlayerCameraState(this);
      BoatState   = new BoatCameraState(this);

      ActiveState = PlayerState;
      Instance    = this;
   }

   public override void OnUpdate(float DeltaTime)
   {
      ActiveState?.OnUpdate();
      
      Vector3 Cam_FWD  = transform.forward;
      m_WorldOrientation = Mathf.Atan2(Cam_FWD.x, Cam_FWD.z) * Mathf.Rad2Deg;
   }

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

   public Camera       Camera       => Controller.Camera;
   public InputManager InputManager => InputManager.Instance;
   
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

   public void UpdateCamera()
   {
      Current = Target;
      Alpha   = 1f;
      
      Camera.fieldOfView        = Current.FOV;
      Camera.transform.forward  = Current.Direction;
      Camera.transform.position = Current.CalculatePosition(Player.Instance.transform.position);
   }
}

public class BoatCameraState : CameraState
{
   private Vector2 m_CameraRot;

   public BoatCameraState(CameraController Controller) : base(Controller) {}

   public override void OnEnter() => m_CameraRot = Vector2.zero;

   public override void OnUpdate()
   {
      Vector2 CameraInput = InputManager.Boat_Camera.Value;
      m_CameraRot.x += CameraInput.x * Controller.LookSensitivity * Time.deltaTime;
      m_CameraRot.y += CameraInput.y * Controller.LookSensitivity * Time.deltaTime;

      m_CameraRot.x = Mathf.Clamp(m_CameraRot.x, Controller.lookXLimit.x, Controller.lookXLimit.y);
      m_CameraRot.y = Mathf.Clamp(m_CameraRot.y, Controller.lookYLimit.x, Controller.lookYLimit.y);

      //Flip the rotations if at limits
      if (m_CameraRot.x > 179.9f) m_CameraRot.x       = -180;
      else if (m_CameraRot.x < -179.9f) m_CameraRot.x = 180;

      Quaternion Camera_Rot     = Quaternion.Euler(m_CameraRot.y, m_CameraRot.x, 0);
      Vector3    Camera_Forward = Camera_Rot * Vector3.forward;
      Vector3    Boat_Pos       = Boat.Instance.transform.position;

      Controller.transform.position = Boat_Pos + (-Camera_Forward * Controller.CameaLength);
      Controller.transform.rotation = Camera_Rot;
   }
}