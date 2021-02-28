using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class EditorLevelLoader
{
   public static void LoadLevel(string Key)
   {
      GameObject     Asset     = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/PF_GameManager.prefab");
      LoadingManager Manager   = Asset.GetComponent<LoadingManager>();
      int            Level_IDx = Manager.GetLevelIDx(Key);

      string[] Scenes = Manager.Levels[Level_IDx].Scenes;
      for (int i = 0; i < Scenes.Length; i++)
         EditorSceneManager.OpenScene(Scenes[i], i == 0 ? OpenSceneMode.Single : OpenSceneMode.Additive);
   }
   
   [MenuItem("Levels/MainMenu")]
   public static void OpenMainMenu() => LoadLevel("MainMenu");
   
   [MenuItem("Levels/BlockOut")]
   public static void OpenBlockOut() => LoadLevel("BlockOut");
}
