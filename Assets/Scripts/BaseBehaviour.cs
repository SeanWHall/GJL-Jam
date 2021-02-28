using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;

//Base Behaviour, that all scene components will implement
//Will allow me to add my own custom update loop since normal Update(); is slow
public class BaseBehaviour : MonoBehaviour
{
   public static List<BaseBehaviour> AllBehaviours       = new List<BaseBehaviour>();
   public static List<BaseBehaviour> Behaviours_ToAdd    = new List<BaseBehaviour>();
   public static List<BaseBehaviour> Behaviours_ToRemove = new List<BaseBehaviour>();
   
   public static GameManager  GameManager  => GameManager.Instance;
   public static InputManager InputManager => InputManager.Instance;
   
   private string _ID;
   public  string ID => !string.IsNullOrEmpty(_ID) ? _ID : (_ID = GetType().Name);

   private CustomSampler _Sample;
   public CustomSampler Sample => _Sample ?? (_Sample = CustomSampler.Create(ID));
   
   public virtual int          Priority    => 0;
   public virtual eUpdateFlags UpdateFlags => eUpdateFlags.None;

   public virtual void OnLevelLoad() {}
   public virtual void OnUpdate(float DeltaTime) {}

   public virtual void OnEnable()  => Behaviours_ToAdd.Add(this);
   public virtual void OnDisable() => Behaviours_ToRemove.Add(this);
   
   public void Log(string Message)   => Debug.Log($"[{ID}] {Message}", this);
   public void Warn(string Message)  => Debug.LogWarning($"[{ID}] {Message}", this);
   public void Error(string Message) => Debug.LogError($"[{ID}] {Message}", this);

   public static int GetBehaviours<T>(IList<T> Behaviours)
   {
      Behaviours.Clear();
      int Behaviours_Len = AllBehaviours.Count;
      for (int i = 0; i < Behaviours_Len; i++)
      {
         if(!(AllBehaviours[i] is T BehaviourTyped))
            continue;
         
         Behaviours.Add(BehaviourTyped);
      }

      return Behaviours.Count;
   }
}

[Flags]
public enum eUpdateFlags
{
   None = 0,
   RequireUpdate = 1 << 1,
   WhileLoading = 1 << 2,
   WhilePaused = 1 << 3,
   WhileDisabled = 1 << 4,
}
