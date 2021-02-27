using UnityEngine;

public class UIPulse : BaseBehaviour
{
    public float          Speed = 2f; //Pulse Speed
    public AnimationCurve GrowAnimation;

    public override eUpdateFlags UpdateFlags => eUpdateFlags.RequireUpdate;

    private Vector2 m_SizeDelta;
    private float   m_Length;
    private float   m_Alpha;

    private void Awake()
    {
        m_SizeDelta = ((RectTransform) transform).sizeDelta;
        m_Length    = GrowAnimation[GrowAnimation.length - 1].time;
    }
    
    public override void OnUpdate(float DeltaTime)
    {
        ((RectTransform) transform).sizeDelta =  m_SizeDelta * GrowAnimation.Evaluate(m_Alpha * m_Length);
        m_Alpha                               += DeltaTime * Speed;
        if (m_Alpha >= 1f)
            m_Alpha = 0;
    }
}
