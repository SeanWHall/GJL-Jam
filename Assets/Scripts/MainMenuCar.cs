using UnityEngine;

public class MainMenuCar : BaseBehaviour
{
    public Transform P1;
    public Transform P2;

    public MeshRenderer[] BodyRenderers;
    public Material[]     CarMats;
    public float          Speed       = 20f;
    public float          MaxIdleTime = 5;
    public bool           StopMoving;
    
    public override eUpdateFlags UpdateFlags => eUpdateFlags.RequireUpdate;

    private Vector3 P1_Pos;
    private Vector3 P2_Pos;
    
    private float m_WaitTime;
    private float m_Alpha;

    public override void OnEnable()
    {
        base.OnEnable();

        P1_Pos = P1.position;
        P2_Pos = P2.position;
    }

    public override void OnUpdate(float DeltaTime)
    {
        if (m_WaitTime > 0f)
        {
            //Wait till the idle time has finished
            m_WaitTime -= DeltaTime;
            return;
        }

        transform.position = Vector3.Lerp(P1_Pos, P2_Pos, m_Alpha);
        m_Alpha = Mathf.Clamp01(m_Alpha + Speed * DeltaTime);

        if (m_Alpha < 1f)
            return;

        if (StopMoving)
        {
            enabled = false;
            return;
        }
        
        m_Alpha    = 0f;
        m_WaitTime = Random.Range(0f, MaxIdleTime);
            
        Material NewMat = CarMats[Random.Range(0, CarMats.Length)];
        foreach (var Renderer in BodyRenderers)
            Renderer.sharedMaterial = NewMat;
    }

    private void OnDrawGizmos()
    {
        if (P1 == null || P2 == null)
            return;

        Gizmos.color = Color.red;
        Gizmos.DrawLine(P1.position, P2.position);
        Gizmos.DrawSphere(P1.position, 0.1f);
        Gizmos.DrawSphere(P2.position, 0.1f);
    }
}
