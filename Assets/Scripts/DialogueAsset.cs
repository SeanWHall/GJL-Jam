using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using System.Reflection;
using UnityEditor;

[CustomEditor(typeof(DialogueAsset))]
public class DialogueAssetEditor : Editor
{
   private delegate object FieldDrawerDel(string Name, object Value);
   
   //Cache fields, so we dont need to do reflection each time the editor repaints
   private static Dictionary<Type, FieldInfo[]>    _Serialized_Fields = new Dictionary<Type, FieldInfo[]>();
   private static Dictionary<Type, FieldDrawerDel> _Type_Drawers      = new Dictionary<Type, FieldDrawerDel>();
   
   private SerializedProperty Participants_Prop;
   private SerializedProperty Nodes_Prop;
   private int                Node_IDx;
   private string[]           Participants_Names = new string[0];
   private string[]           Nodes_Names        = new string[0];
   private Type[]             Action_Types       = new Type[0];
   private string[]           Action_Types_Names = new string[0];
   
   private void OnEnable()
   {
      Participants_Prop = serializedObject.FindProperty("Participants");
      Nodes_Prop        = serializedObject.FindProperty("Nodes");

      Type   Base_Type     = typeof(DialogueAction);
      Type[] All_Types     = Base_Type.Assembly.GetTypes();
      int    All_Types_Len = All_Types.Length;

      int Valid_Types = 0;
      for (int i = 0; i < All_Types_Len; i++)
      {
         Type Current = All_Types[i];
         if(Current == Base_Type || !Current.IsSubclassOf(Base_Type))
            continue;

         FieldInfo[] Fields       = Current.GetFields();
         int         Fields_Len   = Fields.Length;
         int         Valid_Fields = 0;

         for (int f = 0; f < Fields_Len; f++)
         {
            FieldInfo Field = Fields[f];
            if(Field.GetCustomAttribute<HideInInspector>() != null)
               continue;

            Fields[Valid_Fields++] = Field;
         }
         Array.Resize(ref Fields, Valid_Fields);
         
         All_Types[Valid_Types++]    = Current;
         _Serialized_Fields[Current] = Fields;
      }
      Array.Resize(ref All_Types, Valid_Types);
      Action_Types = All_Types;

      Action_Types_Names = new string[Valid_Types];
      for (int i = 0; i < Valid_Types; i++)
         Action_Types_Names[i] = All_Types[i].Name;
      
      _Type_Drawers[typeof(string)] = (Name, Value) => EditorGUILayout.TextField(Name,   (string)Value);
      _Type_Drawers[typeof(int)]    = (Name, Value) => EditorGUILayout.IntField(Name,    (int)Value);
      _Type_Drawers[typeof(double)] = (Name, Value) => EditorGUILayout.DoubleField(Name, (double)Value);
      _Type_Drawers[typeof(float)]  = (Name, Value) => EditorGUILayout.DoubleField(Name, (float)Value);
      _Type_Drawers[typeof(Enum)]   = (Name, Value) => EditorGUILayout.EnumPopup(Name,   (Enum) Value);
      _Type_Drawers[typeof(bool)]   = (Name, Value) => EditorGUILayout.Toggle(Name,      (bool)Value);
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
         SerializedProperty Node_Dialogue   = Node_Elem.FindPropertyRelative("Dialogue");
         SerializedProperty Node_Options    = Node_Elem.FindPropertyRelative("Options");
         SerializedProperty Node_Actions    = Node_Elem.FindPropertyRelative("Actions");
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
         
         Node_AllowLeave.boolValue = EditorGUILayout.Toggle("Allow Leave", Node_AllowLeave.boolValue);

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
            SerializedProperty Option_Elem   = Node_Options.GetArrayElementAtIndex(i);
            SerializedProperty Text_Prop     = Option_Elem.FindPropertyRelative("Text");
            SerializedProperty NextNode_Prop = Option_Elem.FindPropertyRelative("NextNode");
            SerializedProperty Bools_Prop    = Option_Elem.FindPropertyRelative("Bools");
            
            using (new EditorGUILayout.HorizontalScope())
            {
               Option_Elem.isExpanded = EditorGUI.Foldout(EditorGUILayout.GetControlRect(GUILayout.Width(20)), Option_Elem.isExpanded, GUIContent.none);
               Text_Prop.stringValue  = EditorGUILayout.TextField(Text_Prop.stringValue);

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
            
            if(!Option_Elem.isExpanded)
               continue;

            using (new EditorGUI.IndentLevelScope())
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
               int Bools_Len = Bools_Prop.arraySize;
               using (new EditorGUILayout.HorizontalScope())
               {
                  EditorGUILayout.LabelField("Bools:", EditorStyles.boldLabel);
                  GUILayout.FlexibleSpace();
                  if (GUILayout.Button("+", GUILayout.Width(25)))
                     Bools_Prop.arraySize++;
               }

               int Bool_DeleteIDx = -1;

               for (int b = 0; b < Bools_Len; b++)
               {
                  using (new EditorGUILayout.HorizontalScope())
                  {
                     SerializedProperty Bool_Elem           = Bools_Prop.GetArrayElementAtIndex(b);
                     SerializedProperty Bool_Participant    = Bool_Elem.FindPropertyRelative("Participant");
                     SerializedProperty Bool_Key            = Bool_Elem.FindPropertyRelative("Key");
                     SerializedProperty Bool_State          = Bool_Elem.FindPropertyRelative("State");
                     
                     int Participant_IDx     = GetParticipantIDx(Bool_Participant.stringValue);
                     int New_Participant_IDx = EditorGUILayout.Popup(Participant_IDx, Participants_Names, GUILayout.MaxWidth(200));

                     if (Participant_IDx != New_Participant_IDx)
                        Bool_Participant.stringValue = Participants_Names[New_Participant_IDx];

                     Bool_Key.stringValue = EditorGUILayout.TextField(Bool_Key.stringValue, GUILayout.MaxWidth(200));
                     
                     
                     Bool_State.boolValue = EditorGUILayout.Toggle(Bool_State.boolValue, GUILayout.Width(50));
                     
                     GUILayout.FlexibleSpace();
                     if(GUILayout.Button("X", GUILayout.Width(25)))
                        Bool_DeleteIDx = b;
                  }
               }
               
               if(Bool_DeleteIDx != -1)
                  Bools_Prop.DeleteArrayElementAtIndex(Bool_DeleteIDx);
            }
         }
         
         if(NodeOption_DeleteIDx != -1)
            Node_Options.DeleteArrayElementAtIndex(NodeOption_DeleteIDx);
         
         EditorGUILayout.Space();
         
         int Node_Actions_Len = Node_Actions.arraySize;
         using (new EditorGUILayout.HorizontalScope())
         {
            EditorGUILayout.LabelField("Actions:", EditorStyles.boldLabel);
            if (GUILayout.Button("+", GUILayout.Width(25)))
               Node_Actions.arraySize++;
         }

         using (new EditorGUI.IndentLevelScope())
         using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
         {
            int Action_DeleteIDx = -1;
            for (int i = 0; i < Node_Actions_Len; i++)
            {
               SerializedProperty Action_Elem = Node_Actions.GetArrayElementAtIndex(i);
               SerializedProperty Type_Prop   = Action_Elem.FindPropertyRelative("Type");
               SerializedProperty JSON_Prop   = Action_Elem.FindPropertyRelative("JSon");
               Type               ActionType  = null;

               using (new EditorGUILayout.HorizontalScope())
               {
                  int Type_IDx     = GetActionTypeIDx(Type_Prop.stringValue);
                  int New_Type_IDx = EditorGUILayout.Popup(Type_IDx, Action_Types_Names);

                  if (Type_IDx != New_Type_IDx)
                  {
                     Type_Prop.stringValue = Action_Types[New_Type_IDx].AssemblyQualifiedName;
                     JSON_Prop.stringValue = String.Empty;
                  }

                  if (New_Type_IDx != -1)
                     ActionType = Action_Types[New_Type_IDx];

                  if (GUILayout.Button("X", GUILayout.Width(25)))
                     Action_DeleteIDx = i;
               }

               if (ActionType == null)
                  continue;

               string         Action_JSON          = JSON_Prop.stringValue;
               DialogueAction Action_Object        = (DialogueAction) (string.IsNullOrEmpty(Action_JSON) ? Activator.CreateInstance(ActionType) : JsonUtility.FromJson(Action_JSON, ActionType));
               FieldInfo[]    SerializedFields     = _Serialized_Fields[ActionType];
               int            SerializedFields_Len = SerializedFields.Length;

               int Participant_IDx     = GetParticipantIDx(Action_Object.Participant);
               int New_Participant_IDx = EditorGUILayout.Popup("Participant", Participant_IDx, Participants_Names);

               if (Participant_IDx != New_Participant_IDx)
                  Action_Object.Participant = Participants_Names[New_Participant_IDx];

               for (int i_2 = 0; i_2 < SerializedFields_Len; i_2++)
               {
                  FieldInfo Field = SerializedFields[i_2];
                  if (!_Type_Drawers.TryGetValue(Field.FieldType, out FieldDrawerDel Drawer))
                     continue; //Ignore types which dont have a drawer

                  Field.SetValue(Action_Object, Drawer(Field.Name, Field.GetValue(Action_Object)));
               }

               JSON_Prop.stringValue = JsonUtility.ToJson(Action_Object);
            }
            
            if(Action_DeleteIDx != -1)
               Node_Actions.DeleteArrayElementAtIndex(Action_DeleteIDx);
         }
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

   private int GetActionTypeIDx(string TypeName)
   {
      int Len = Action_Types.Length;
      for (int i = 0; i < Len; i++)
      {
         if (Action_Types[i].AssemblyQualifiedName == TypeName)
            return i;
      }

      return -1;
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

[Serializable]
public class DialogueNode
{
   public string             Name; //Used to find Node
   public string             Dialogue;
   public bool               AllowLeave = true;
   public DialogueOption[]   Options = new DialogueOption[0];
   public SerializedAction[] Actions = new SerializedAction[0];
}

[Serializable]
public class DialogueOption
{
   public string             Text;
   public string             NextNode;
   public ParticipantBools[] Bools = new ParticipantBools[0]; //Requires these bools to be shown
}

[Serializable]
public struct ParticipantBools
{
   public string Participant;
   public string Key;
   public bool   State;
}

[Serializable]
public class SerializedAction
{
   public string Type;
   public string JSon;

   [NonSerialized]
   private DialogueAction _Action;
   public DialogueAction Action => _Action ?? (_Action = (DialogueAction)JsonUtility.FromJson(JSon, System.Type.GetType(Type)));
}

[Serializable]
public abstract class DialogueAction
{
   [HideInInspector]
   public string Participant;
   public abstract void Trigger(Character Context);
}

public class DialogueAnimationAction : DialogueAction
{
   public string StateName;

   public override void Trigger(Character Context)
   {
      Debug.Log($"Playing State: {StateName} On Character: {Context.Name}");
      Context.AnimController.Play(StateName);
   }
}

public class DialogueSetBoolAction : DialogueAction
{
   public string Key;
   public bool   Value;

   public override void Trigger(Character Context)
   {
      Debug.Log($"Setting Dialogue Bool: {Key} = {Value} On Character: {Context.Name}");
      
      int IDx = Context.GetDialogueBoolIDx(Key);
      if (IDx == -1)
         return;

      //Update Bool on Participant
      Context.Bools[IDx].Value = Value;
   }
}