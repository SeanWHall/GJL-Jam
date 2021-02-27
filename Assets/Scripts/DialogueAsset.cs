using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Experimental.GraphView;

[CustomEditor(typeof(DialogueAsset))]
public class DialogueAssetEditor : Editor
{
   private SerializedProperty Participants_Prop;
   private SerializedProperty Nodes_Prop;
   private int                Node_IDx;
   private string[]           Participants_Names = new string[0];
   private string[]           Nodes_Names        = new string[0];
   
   private void OnEnable()
   {
      Participants_Prop = serializedObject.FindProperty("Participants");
      Nodes_Prop        = serializedObject.FindProperty("Nodes");
   }

   public override void OnInspectorGUI()
   {
      serializedObject.Update();
      DrawParticipants();
      EditorGUILayout.Space();
      UpdateNodeNames();
      DrawNode();
      serializedObject.ApplyModifiedProperties();
   }

   private void DrawParticipants()
   {
      int Participants_Len       = Participants_Prop.arraySize;
      int Participants_DeleteIDx = -1;
      using (new EditorGUILayout.HorizontalScope())
      {
         EditorGUILayout.LabelField("Participants", EditorStyles.boldLabel);
         if(GUILayout.Button("+", GUILayout.Width(25)))
            Participants_Prop.InsertArrayElementAtIndex(Participants_Len);
      }

      if (Participants_Len == 0)
      {
         EditorGUILayout.HelpBox("Dialogue must have participants", MessageType.Error);
         return;
      }
      
      if(Participants_Names.Length != Participants_Len)
         Array.Resize(ref Participants_Names, Participants_Len);
      
      using (new EditorGUI.IndentLevelScope())
      {
         for (int i = 0; i < Participants_Len; i++)
         {
            using (new EditorGUILayout.HorizontalScope())
            {
               SerializedProperty Participant_Elem = Participants_Prop.GetArrayElementAtIndex(i);
               Participant_Elem.stringValue = Participants_Names[i] = EditorGUILayout.TextField(Participant_Elem.stringValue);
               if (GUILayout.Button("X", GUILayout.Width(25)))
                  Participants_DeleteIDx = i;
            }
         }
      }
      
      if (Participants_DeleteIDx != -1)
         Participants_Prop.DeleteArrayElementAtIndex(Participants_DeleteIDx);
   }

   private void UpdateNodeNames()
   {
      int Nodes_Len = Nodes_Prop.arraySize;
      
      if(Nodes_Names.Length != Nodes_Len)
         Array.Resize(ref Nodes_Names, Nodes_Len);
      
      for(int i = 0; i < Nodes_Len; i++)
      {
         SerializedProperty Node_Elem = Nodes_Prop.GetArrayElementAtIndex(i);
         SerializedProperty Node_Name = Node_Elem.FindPropertyRelative("Name");

         Nodes_Names[i] = Node_Name.stringValue;
      }
   }
   
   private void DrawNode()
   {
      int  Nodes_Len  = Nodes_Prop.arraySize;
      bool DeleteNode = false;
      using (new EditorGUILayout.HorizontalScope())
      {
         EditorGUILayout.LabelField("Nodes", EditorStyles.boldLabel, GUILayout.Width(75));
         Node_IDx = EditorGUILayout.Popup(Node_IDx, Nodes_Names);
         if (GUILayout.Button("+", GUILayout.Width(25)))
         {
            Nodes_Prop.arraySize = Nodes_Len + 1;
            
            SerializedProperty New_Node = Nodes_Prop.GetArrayElementAtIndex(Nodes_Len);
            New_Node.FindPropertyRelative("Name").stringValue     = $"New Node {Nodes_Len}";
            New_Node.FindPropertyRelative("Speaker").stringValue  = string.Empty;
            New_Node.FindPropertyRelative("Dialogue").stringValue = string.Empty;
            New_Node.FindPropertyRelative("Options").arraySize    = 0;
            
            Repaint();
         }
      }

      if (Nodes_Len == 0)
      {
         EditorGUILayout.HelpBox("Dialogue must have nodes", MessageType.Error);
         return;
      }

      using (new EditorGUI.IndentLevelScope())
      {
         SerializedProperty Node_Elem       = Nodes_Prop.GetArrayElementAtIndex(Node_IDx);
         SerializedProperty Node_Name       = Node_Elem.FindPropertyRelative("Name");
         SerializedProperty Node_Speaker    = Node_Elem.FindPropertyRelative("Speaker");
         SerializedProperty Node_Dialogue   = Node_Elem.FindPropertyRelative("Dialogue");
         SerializedProperty Node_Options    = Node_Elem.FindPropertyRelative("Options");
         SerializedProperty Node_AllowLeave = Node_Elem.FindPropertyRelative("AllowLeave");
         
         using (new EditorGUILayout.HorizontalScope())
         {
            string Old_NodeName = Node_Name.stringValue;
            string New_NodeName = EditorGUILayout.DelayedTextField(Old_NodeName);
            
            if (Old_NodeName != New_NodeName)
            {
               Node_Name.stringValue = New_NodeName;
               UpdateDialogueOptions(Old_NodeName, New_NodeName);
            }
            
            DeleteNode = GUILayout.Button("Delete", GUILayout.Width(60)) && EditorUtility.DisplayDialog("Dialogue", $"You are about to delete Node: {Node_Name.name}, Are you sure?", "Yep", "Nope");
         }

         int Speaker_IDx     = GetParticipantIDx(Node_Speaker.stringValue);
         int New_Speaker_IDx = EditorGUILayout.Popup("Speaker", Speaker_IDx, Participants_Names);

         Node_AllowLeave.boolValue = EditorGUILayout.Toggle("Allow Leave", Node_AllowLeave.boolValue);

         if (Speaker_IDx != New_Speaker_IDx)
            Node_Speaker.stringValue = Participants_Names[New_Speaker_IDx];
         
         EditorGUILayout.Space();

         EditorGUILayout.LabelField("Dialogue:", EditorStyles.boldLabel);
         Node_Dialogue.stringValue = EditorGUILayout.TextArea(Node_Dialogue.stringValue, GUILayout.Height(200));

         int Node_Options_Len = Node_Options.arraySize;
         using (new EditorGUILayout.HorizontalScope())
         {
            EditorGUILayout.LabelField("Options:", EditorStyles.boldLabel);
            if (GUILayout.Button("+", GUILayout.Width(25)))
               Node_Options.arraySize++;
         }

         int NodeOption_DeleteIDx = -1;
         for (int i = 0; i < Node_Options_Len; i++)
         {
            using (new EditorGUILayout.HorizontalScope())
            {
               SerializedProperty Option_Elem   = Node_Options.GetArrayElementAtIndex(i);
               SerializedProperty Text_Prop     = Option_Elem.FindPropertyRelative("Text");
               SerializedProperty NextNode_Prop = Option_Elem.FindPropertyRelative("NextNode");
               
               Text_Prop.stringValue = EditorGUILayout.TextField(Text_Prop.stringValue);

               int NextNode_IDx     = GetNodeIDx(NextNode_Prop.stringValue);
               int New_NextNode_IDx = EditorGUILayout.Popup(NextNode_IDx, Nodes_Names);

               if (NextNode_IDx != New_NextNode_IDx)
                  NextNode_Prop.stringValue = Nodes_Names[New_NextNode_IDx];

               GUI.enabled = New_NextNode_IDx != -1;
               if (GUILayout.Button("Go To", GUILayout.Width(50)))
               {
                  Node_IDx = New_NextNode_IDx;
                  Repaint();
               }
               GUI.enabled = true;
               if (GUILayout.Button("X", GUILayout.Width(25)))
                  NodeOption_DeleteIDx = i;
            }
         }
         
         if(NodeOption_DeleteIDx != -1)
            Node_Options.DeleteArrayElementAtIndex(NodeOption_DeleteIDx);
      }
      
      if (DeleteNode)
      {
         Nodes_Prop.DeleteArrayElementAtIndex(Node_IDx);
         Node_IDx = 0; //Reset Node IDx
      }
   }

   //Updates Options pointing to the old node name
   private void UpdateDialogueOptions(string OldNodeName, string NewNodeName)
   {
      int Len = Nodes_Prop.arraySize;
      for (int i = 0; i < Len; i++)
      {
         SerializedProperty Node_Elem    = Nodes_Prop.GetArrayElementAtIndex(i);
         SerializedProperty Options_Prop = Node_Elem.FindPropertyRelative("Options");

         int Options_Len = Options_Prop.arraySize;
         for (int i_2 = 0; i_2 < Options_Len; i_2++)
         {
            SerializedProperty Options_Elem  = Options_Prop.GetArrayElementAtIndex(i_2);
            SerializedProperty NextNode_Prop = Options_Elem.FindPropertyRelative("NextNode");

            if (NextNode_Prop.stringValue == OldNodeName)
               NextNode_Prop.stringValue = NewNodeName;
         }
      }
   }
   

   private int GetNodeIDx(string Name)
   {
      int Len = Nodes_Names.Length;
      for (int i = 0; i < Len; i++)
      {
         if (Name == Nodes_Names[i])
            return i;
      }

      return -1;
   }

   private int GetParticipantIDx(string Name)
   {
      int Len = Participants_Names.Length;
      for (int i = 0; i < Len; i++)
      {
         if (Name == Participants_Names[i])
            return i;
      }

      return -1;
   }
}
#endif

//Very Rough Dialogue Asset
[CreateAssetMenu]
public class DialogueAsset : ScriptableObject
{
   public string[]       Participants; //used to validate against
   public DialogueNode[] Nodes;

   public int GetNodeIDx(string Name)
   {
      int Len = Nodes.Length;
      for (int i = 0; i < Len; i++)
      {
         if (Nodes[i].Name == Name)
            return i;
      }

      return -1;
   }
   
   public bool HasParticipant(string Name)
   {
      int Len = Participants.Length;
      for (int i = 0; i < Len; i++)
      {
         if (Participants[i] == Name)
            return true;
      }

      return false;
   }
}

[System.Serializable]
public class DialogueNode
{
   //TODO: Add Animations & Sounds to go with dialogue
   public string           Name; //Used to find Node
   public string           Speaker;
   public string           Dialogue;
   public bool             AllowLeave = true;
   public DialogueOption[] Options;
}

[System.Serializable]
public class DialogueOption
{
   //TODO: Add conditional checks, so this option is only shown when the condition is met
   public string Text;
   public string NextNode;
}