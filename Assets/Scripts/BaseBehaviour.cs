using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Base Behaviour, that all scene components will implement
//Will allow me to add my own custom update loop since normal Update(); is slow
public class BaseBehaviour : MonoBehaviour
{
   private string _ID;
   public  string ID => !string.IsNullOrEmpty(_ID) ? _ID : (_ID = GetType().Name);

   public void Log(string Message)   => Debug.Log($"[{ID}] {Message}");
   public void Warn(string Message)  => Debug.LogWarning($"[{ID}] {Message}");
   public void Error(string Message) => Debug.LogError($"[{ID}] {Message}");
}
