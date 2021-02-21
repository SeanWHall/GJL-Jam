using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Base Behaviour, that all scene components will implement
//Will allow me to add my own custom update loop since normal Update(); is slow
public class BaseBehaviour : MonoBehaviour
{
   public static List<BaseBehaviour> AllBehaviours = new List<BaseBehaviour>();
   
   private string _ID;
   public  string ID => !string.IsNullOrEmpty(_ID) ? _ID : (_ID = GetType().Name);
   
   public virtual eUpdateFlags UpdateFlags => eUpdateFlags.None;

   public virtual void OnLevelLoad() {}
   public virtual void OnUpdate(float DeltaTime) {}

   public virtual void OnEnable()  => AllBehaviours.Add(this);
   public virtual void OnDisable() => AllBehaviours.Remove(this);

   public void Log(string Message)   => Debug.Log($"[{ID}] {Message}");
   public void Warn(string Message)  => Debug.LogWarning($"[{ID}] {Message}");
   public void Error(string Message) => Debug.LogError($"[{ID}] {Message}");
}

public enum eUpdateFlags
{
   None = 0,
   RequireUpdate = 1 << 1,
   WhileLoading = 1 << 2,
   WhilePaused = 1 << 3,
}
