using System;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;

[CustomEditor(typeof(LoadingAsset))]
public class LoadingAssetEditor : Editor
{
   private SerializedProperty Levels_Prop;

   private void OnEnable() => Levels_Prop = serializedObject.FindProperty("Levels");

   public override void OnInspectorGUI()
   {
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

[CreateAssetMenu]
public class LoadingAsset : ScriptableObject
{
   public LevelCollection[] Levels = new LevelCollection[0];
}

[Serializable]
public class LevelCollection
{
   public string Key;
   public string[] Scenes;
}
