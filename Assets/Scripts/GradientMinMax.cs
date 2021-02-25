using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;

[CustomEditor(typeof(GradientMinMax))]
public class GradientMinMaxEditor : Editor
{
    private static List<MeshRenderer> _Renderers = new List<MeshRenderer>();
    private static List<Vector3>      _Verts     = new List<Vector3>();

    private SerializedProperty _MinMax;

    private GradientMinMax Instance => (GradientMinMax) target;
    
    private void OnEnable() => _MinMax = serializedObject.FindProperty("_MinMax");

    public override void OnInspectorGUI()
    {
        EditorGUILayout.HelpBox("Calculates the Min & Max of all meshes under this component, used to apply gradients along meshes", MessageType.Info);
        if (GUILayout.Button("Calculate MinMax"))
        {
            serializedObject.Update();
            
            Vector2 MinMax = new Vector2(float.PositiveInfinity, float.NegativeInfinity);
            Instance.GetComponentsInChildren(true, _Renderers);
            int Renderers_Len = _Renderers.Count;
            for (int i = 0; i < Renderers_Len; i++)
            {
                MeshFilter Filter = _Renderers[i].GetComponent<MeshFilter>();
                if(Filter == null)
                    continue;

                Mesh Target = Filter.sharedMesh;
                if(Target == null)
                    continue;

                Matrix4x4 LTW = Filter.transform.localToWorldMatrix;
                Target.GetVertices(_Verts);
                int Verts_Len = _Verts.Count;

                for (int v = 0; v < Verts_Len; v++)
                {
                    float Y = LTW.MultiplyPoint3x4(_Verts[v]).y;
                    MinMax.x = Mathf.Min(MinMax.x, Y);
                    MinMax.y = Mathf.Max(MinMax.y, Y);
                }
            }

            _MinMax.vector2Value = MinMax;
            
            serializedObject.ApplyModifiedProperties();
            serializedObject.Update();

            Instance.SetupPropertyBlock();
        }
    }
}
#endif

[ExecuteInEditMode]
public class GradientMinMax : MonoBehaviour
{
    private static List<MeshRenderer> _Renderers = new List<MeshRenderer>();
    
    private MaterialPropertyBlock m_Block;
    
    [HideInInspector, SerializeField]
    private Vector2 _MinMax;
    
    public void SetupPropertyBlock()
    {
        if (m_Block == null)
            m_Block = new MaterialPropertyBlock();
        
        m_Block.SetVector("_GradMinMax", _MinMax);

        GetComponentsInChildren(true, _Renderers);
        int Renderers_Len = _Renderers.Count;
        for(int i = 0; i < Renderers_Len; i++)
            _Renderers[i].SetPropertyBlock(m_Block);
    }

    private void OnEnable()   => SetupPropertyBlock();
    private void OnValidate() => SetupPropertyBlock();
}
