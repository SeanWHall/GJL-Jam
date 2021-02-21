using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

   private void Setup()
   {
      Log("GameManager Setup");
   }
}
