using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;

[CustomEditor(typeof(LoadingManager))]
public class LoadingManagerEditor : Editor
{
   private SerializedProperty Levels_Prop;

   private void OnEnable() => Levels_Prop = serializedObject.FindProperty("Levels");

   public override void OnInspectorGUI()
   {
      base.OnInspectorGUI();
      
      EditorGUILayout.Space();
      
      using (new EditorGUILayout.HorizontalScope())
      {
         EditorGUILayout.LabelField("Levels", EditorStyles.boldLabel);
         GUILayout.FlexibleSpace();
         if (GUILayout.Button("New Level"))
            Levels_Prop.arraySize++;
      }

      int Level_RemoveIDx = -1;
      int Levels_Len      = Levels_Prop.arraySize;
      for (int i = 0; i < Levels_Len; i++)
      {
         SerializedProperty Level_Elem  = Levels_Prop.GetArrayElementAtIndex(i);
         SerializedProperty Key    = Level_Elem.FindPropertyRelative("Key");
         SerializedProperty Scenes = Level_Elem.FindPropertyRelative("Scenes");
         using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
         {
            Rect FoldOut_Rect  = EditorGUILayout.GetControlRect();
            FoldOut_Rect.xMin += 10;
            
            Level_Elem.isExpanded =  EditorGUI.Foldout(FoldOut_Rect, Level_Elem.isExpanded, string.IsNullOrEmpty(Key.stringValue) ? "[New Collection]" : Key.stringValue, true);
            
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("X", EditorStyles.toolbarButton, GUILayout.Width(25)))
               Level_RemoveIDx = i;
         }
         
         if(!Level_Elem.isExpanded)
            continue;

         using (new EditorGUI.IndentLevelScope())
         {
            EditorGUILayout.PropertyField(Key);
            using (new EditorGUILayout.HorizontalScope())
            {
               EditorGUILayout.LabelField("Scenes");
               GUILayout.FlexibleSpace();
               if (GUILayout.Button("+", GUILayout.Width(25)))
                  Scenes.arraySize++;
            }

            int Scenes_RemoveIDx = -1;
            int Scenes_Len       = Scenes.arraySize;
            for (int s = 0; s < Scenes_Len; s++)
            {
               using (new EditorGUILayout.HorizontalScope())
               {
                  SerializedProperty Scene_Elem     = Scenes.GetArrayElementAtIndex(s);
                  string             Scene_Path     = Scene_Elem.stringValue;
                  SceneAsset         Scene_Asset    = AssetDatabase.LoadAssetAtPath<SceneAsset>(Scene_Path);
                  SceneAsset         Scene_NewAsset = (SceneAsset) EditorGUILayout.ObjectField(Scene_Asset, typeof(SceneAsset), false);

                  if (Scene_NewAsset != Scene_Asset)
                  {
                     if (Scene_NewAsset == null) Scene_Elem.stringValue = String.Empty;
                     else Scene_Elem.stringValue                        = AssetDatabase.GetAssetOrScenePath(Scene_NewAsset);
                  }

                  if (GUILayout.Button("X", GUILayout.Width(25)))
                     Scenes_RemoveIDx = s;
               }
               
            }
            
            if(Scenes_RemoveIDx != -1)
               Scenes.DeleteArrayElementAtIndex(Scenes_RemoveIDx);
         }
      }

      if (Level_RemoveIDx != -1)
         Levels_Prop.DeleteArrayElementAtIndex(Level_RemoveIDx);

      serializedObject.ApplyModifiedProperties();
   }
}
#endif

public class LoadingManager : BaseBehaviour
{
   public static LoadingManager Instance { get; private set; }
   public static bool           IsBusy   => Instance != null && Instance.m_Routine != null;

   public float             FadeSpeed = 2f;
   public GameObject        LoadingScreen_Root;
   [HideInInspector]
   public LevelCollection[] Levels = new LevelCollection[0];

   private Coroutine m_Routine;
   private Graphic[] m_Graphics;
   private float[]   m_Graphics_Alphas;
   
   private void Awake()
   {
      if (Instance != null)
      {
         Error("A Loading Manager is already setup?! Something has gone wrong here");
         return;
      }
      
      Instance   = this;
      m_Graphics = LoadingScreen_Root.GetComponents<Graphic>();

      //Cache the Starting alpha of the loading screen elements
      int Graphics_Len = m_Graphics.Length;
      m_Graphics_Alphas = new float[Graphics_Len];
      for (int i = 0; i < Graphics_Len; i++)
         m_Graphics_Alphas[i] = m_Graphics[i].color.a;
      
      LoadingScreen_Root.SetActive(false);
   }
   
   public int GetLevelIDx(string Key)
   {
      int Len = Levels.Length;
      for (int i = 0; i < Len; i++)
      {
         if(Levels[i] == null || Levels[i].Key != Key)
            continue;
         return i;
      }

      return -1;
   }

   public void LoadLevel(string Key)
   {
      if (IsBusy)
      {
         Error($"Failed to Start loading: {Key} as we are already loading something");
         return;
      }

      int LevelIDx = GetLevelIDx(Key);
      if (LevelIDx == -1)
      {
         Error($"Failed to Start loading: {Key} as we couldn't find the collection");
         return;
      }

      LevelCollection Level = Levels[LevelIDx];
      m_Routine = StartCoroutine(LoadLevelASync());
      
      IEnumerator LoadLevelASync()
      {
         yield return FadeLoadingScreen(true); //Show Loading Screen
         
         //Unload the Current Scenes
         Scene[] LoadedScenes     = GetLoadedScenes();
         int     LoadedScenes_Len = LoadedScenes.Length;
         for (int i = 0; i < LoadedScenes_Len; i++)
            yield return SceneManager.UnloadSceneAsync(i);
         
         //Clean memory
         yield return Resources.UnloadUnusedAssets();
         
         GC.Collect();
         GC.WaitForPendingFinalizers();
         
         //Load new Scenes
         int Scenes_Len = Level.Length;
         for (int i = 0; i < Scenes_Len; i++)
            yield return SceneManager.LoadSceneAsync(Level[i], i == 0 ? LoadSceneMode.Single : LoadSceneMode.Additive);
         
         yield return FadeLoadingScreen(false); //Hide Loading Screen
      }
   }

   private IEnumerator FadeLoadingScreen(bool FadeIn)
   {
      float Alpha        = 0;
      int   Graphics_Len = m_Graphics.Length;
      
      UpdateAlpha(); //Setup the alpha before unhiding the loading screen
      LoadingScreen_Root.SetActive(true);
      
      while (Alpha < 1f)
      {
         UpdateAlpha();
         yield return new WaitForEndOfFrame();
         Alpha += FadeSpeed * Time.deltaTime;
      }

      Alpha = 1f;
      UpdateAlpha();
      
      if(!FadeIn) //If we are fading out, hide the loading screen
         LoadingScreen_Root.SetActive(false);

      void UpdateAlpha()
      {
         for (int i = 0; i < Graphics_Len; i++)
         {
            float Start = FadeIn ? 0 : m_Graphics_Alphas[i];
            float End   = FadeIn ? m_Graphics_Alphas[i] : 0;
            
            Graphic Target     = m_Graphics[i];
            Color   Target_Col = Target.color;
            Target_Col.a = Mathf.Lerp(Start, End, Alpha);
            Target.color = Target_Col;
         }
      }
   }

   private Scene[] GetLoadedScenes()
   {
      int     Loaded_Len = SceneManager.sceneCount;
      Scene[] Loaded     = new Scene[Loaded_Len];
      for (int i = 0; i < Loaded_Len; i++)
         Loaded[i] = SceneManager.GetSceneAt(i);

      return Loaded;
   }
}

[Serializable]
public class LevelCollection
{
   public string   Key;
   public string[] Scenes;

   public int Length => Scenes.Length;
   public string this[int i] => Scenes[i];
}