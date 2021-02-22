using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PauseMenu : BaseBehaviour
{
    public static PauseMenu Instance { get; private set; }
    public static bool      IsPaused => Instance != null && Instance.m_Paused;

    public override eUpdateFlags UpdateFlags => eUpdateFlags.RequireUpdate | eUpdateFlags.WhileDisabled | eUpdateFlags.WhilePaused;

    public GameObject Target;

    private TimeScaleHandle m_ScaleHandle;
    private bool            m_Paused;
    
    public override void OnUpdate(float DeltaTime)
    {
        if (!InputManager.Pause.IsPressedOrHeld)
            return;

        Pause(!m_Paused);
    }

    public override void OnEnable()
    {
        base.OnEnable();
        
        //Hide pause Menu
        Target.SetActive(false);
    }

    public void Pause(bool NewPause)
    {
        if (NewPause == m_Paused)
            return;

        if (NewPause) m_ScaleHandle = GameManager.ControlTimeScale(100, 0f);
        else          m_ScaleHandle.Release();
        
        Target.SetActive(NewPause);
        m_Paused = NewPause;
    }
}
