using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.GlobalIllumination;
using UnityEngine.Profiling;

public class GameManager : BaseBehaviour
{
   public static GameManager Instance { get; private set; }
   
   [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
   static void Init()
   {
      GameObject GameManager_Prefab = Resources.Load<GameObject>("PF_GameManager");
      if (GameManager_Prefab == null)
      {
         Debug.LogWarning("Failed to find GameManager prefab, parts of the game wont work");
         return;
      }

      GameObject GameManager_Instance = Instantiate(GameManager_Prefab);
      Instance = GameManager_Instance.GetComponent<GameManager>();
      if (Instance == null)
      {
         Debug.LogWarning("GameManager Prefab doesn't have GameManager component on it!");
         Destroy(GameManager_Instance);
         return;
      }
      
      //Place Gamemanager into DontDestoryOnLoad Scene, so it is persistant across all levels
      DontDestroyOnLoad(GameManager_Instance);
      Instance.Setup();
   }

   private List<TimeScaleHandle> m_ScaleHandles    = new List<TimeScaleHandle>();
   private int                   m_ScaleHandles_Count;

   private void Update()
   {
      int ToAdd_Len = Behaviours_ToAdd.Count;
      if (ToAdd_Len > 0)
      {
         for(int i = 0; i < ToAdd_Len; i++)
            AllBehaviours.Add(Behaviours_ToAdd[i]);
         Behaviours_ToAdd.Clear();
         
         AllBehaviours.Sort((A, B) => B.Priority.CompareTo(A.Priority)); //Sort so Highest priority is first!
      }
      
      int   Behaviours_Len = AllBehaviours.Count;
      float DeltaTime      = Time.deltaTime;
      for (int i = 0; i < Behaviours_Len; i++)
      {
         BaseBehaviour Behaviour = AllBehaviours[i];
         if(Behaviour == this)
            continue; //Dont update the GameManager
         
         try
         {
            //Control whether or not the behaviour should be updated
            eUpdateFlags Flags = Behaviour.UpdateFlags;
            if(!Flags.HasFlag(eUpdateFlags.RequireUpdate))
               continue;
            
            if(LoadingManager.IsBusy && !Flags.HasFlag(eUpdateFlags.WhileLoading))
               continue; //Check if the behaviour wants to be updated while loading
            
            if(PauseMenu.IsPaused && !Flags.HasFlag(eUpdateFlags.WhilePaused))
               continue; //Check if the behaviour wants to be updated while loading
            
            if((!Behaviour.enabled || !Behaviour.gameObject.activeInHierarchy) && !Flags.HasFlag(eUpdateFlags.WhileDisabled))
               continue; //Check if it should update even if the gameobject is disabled
            
            Behaviour.Sample.Begin();
            Behaviour.OnUpdate(DeltaTime);
            Behaviour.Sample.End();
         }
         catch (Exception Ex) { Behaviour.Error(Ex.ToString());}
      }
      
      int ToRemove_Len = Behaviours_ToRemove.Count;
      if (ToRemove_Len > 0)
      {
         for(int i = 0; i < ToAdd_Len; i++)
            AllBehaviours.Remove(Behaviours_ToRemove[i]);
         Behaviours_ToRemove.Clear();
         
         AllBehaviours.Sort((A, B) => B.Priority.CompareTo(A.Priority)); //Sort so Highest priority is first!
      }
   }
   
   private void Setup()
   {
      Log("GameManager Setup");
   }

   //Timescale needs to be monitored and controlled, otherwise gameplay code could very easily soft lock the game
   public void ReleaseTimeScaleHandle(TimeScaleHandle Handle)
   {
      if (!m_ScaleHandles.Remove(Handle))
         return;
      
      RefreshTimeScale(true);
   }
   
   public TimeScaleHandle ControlTimeScale(int Priority, float Scale)
   {
      TimeScaleHandle Handle = new TimeScaleHandle(m_ScaleHandles_Count++, Scale, Priority);
      m_ScaleHandles.Add(Handle);
      RefreshTimeScale(true);
      return Handle;
   }

   public void RefreshTimeScale(bool Sort)
   {
      if(Sort)
         m_ScaleHandles.Sort((A, B) => B.Priority.CompareTo(A.Priority)); //Sort so Highest priority is first!

      //If we have some scale handlers, then use the first one. Otherwise default to zero
      Time.timeScale = m_ScaleHandles.Count > 0 ? m_ScaleHandles[0].Scale : 1f;
   }
}

public struct TimeScaleHandle
{
   private int Handle;
   
   //Limit setting of these properties. Must be done though setters, to improve perf
   public float Scale    { get; private set; }
   public int   Priority { get; private set; }

   public TimeScaleHandle(int Handle, float Scale, int Priority)
   {
      this.Handle   = Handle;
      this.Scale    = Scale;
      this.Priority = Priority;
   }
   
   public void SetScale(float NewScale)
   {
      if (Scale == NewScale)
         return; //Scales are the same, we dont need to update it

      Scale = NewScale;
      GameManager.Instance.RefreshTimeScale(false);
   }

   public void SetPriority(int NewPriority)
   {
      if (Priority == NewPriority)
         return;

      Priority = NewPriority;
      GameManager.Instance.RefreshTimeScale(true);
   }

   public void Release() => GameManager.Instance.ReleaseTimeScaleHandle(this);

   public override int GetHashCode() => Handle.GetHashCode();
}