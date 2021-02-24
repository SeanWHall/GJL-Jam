using System;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;

[CustomEditor(typeof(CameraZone))]
public class CameraZoneEditor : Editor
{
    public CameraZone Zone => (CameraZone)target;
    
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        serializedObject.Update();
        if (GUILayout.Button("Use Scene View"))
        {
            Plane     CamZone_Plane = new Plane(Vector3.up, Zone.transform.position);
            Transform SceneCam      = ((SceneView) SceneView.sceneViews[0]).camera.transform;

            if (CamZone_Plane.Raycast(new Ray(SceneCam.position, SceneCam.forward), out float Dist))
            {
                SerializedProperty Params_Prop           = serializedObject.FindProperty("Params");
                SerializedProperty Params_Direction_Prop = Params_Prop.FindPropertyRelative("Direction");
                SerializedProperty Params_Distance_Prop = Params_Prop.FindPropertyRelative("Distance");

                Params_Distance_Prop.floatValue    = Dist;
                Params_Direction_Prop.vector3Value = SceneCam.forward;
            }
        }
        serializedObject.ApplyModifiedProperties();
    }
}
#endif

public class CameraZone : BaseBehaviour
{
    public CamParams Params           = CamParams.Default;
    public float     InterpolateSpeed = 0.2f;

    private void OnTriggerEnter(Collider other)
    {
        if (!IsPlayer(other))
            return;
        
        CameraController.Instance.PlayerState.LerpTo(Params, InterpolateSpeed);
    }

    private bool IsPlayer(Collider Col) => Col.GetComponentInChildren<Collider>() != null || Col.GetComponentInParent<Collider>() != null;

    private void OnDrawGizmos()
    {
        Quaternion Camera_Rot    = Quaternion.LookRotation(Params.Direction);
        Vector3    Camera_Pos    = Params.CalculatePosition(transform.position);
        Matrix4x4  Camera_Matrix = Matrix4x4.TRS(Camera_Pos, Camera_Rot, Vector3.one);
        
        Gizmos.DrawLine(transform.position, Camera_Pos);
        Gizmos.DrawSphere(transform.position, 0.25f);

        Gizmos.matrix = Camera_Matrix;
        Gizmos.DrawCube(Vector3.zero, new Vector3(0.5f, 0.75f, 1f));
    }
}

