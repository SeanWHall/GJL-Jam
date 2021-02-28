using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PauseMenu : BaseBehaviour
{
    public static PauseMenu Instance { get; private set; }
    public static bool      IsPaused => Instance != null && Instance.m_Paused;

    public override eUpdateFlags UpdateFlags => eUpdateFlags.RequireUpdate | eUpdateFlags.WhileDisabled | eUpdateFlags.WhilePaused;

    public GameObject   Target;
    public GameObject[] Tabs;

    private TimeScaleHandle m_ScaleHandle;
    private bool            m_Paused;
    private float           m_NextPauseTime;
    
    public override void OnUpdate(float DeltaTime)
    {
        if (!InputManager.UI_Pause.IsPressed)
            return;

        Pause(!m_Paused);
    }

    public override void OnEnable()
    {
        base.OnEnable();
        
        //Hide pause Menu
        Target.SetActive(false);
    }

    public void ChangeTab(int IDx)
    {
        Log($"Change Pause Tab: {IDx}");
        int Tabs_Len = Tabs.Length;
        for (int i = 0; i < Tabs_Len; i++)
            Tabs[i].SetActive(i == IDx);
    }

    public void Pause(bool NewPause)
    {
        if (NewPause == m_Paused || Time.unscaledTime < m_NextPauseTime)
            return;

        if (NewPause) m_ScaleHandle = GameManager.ControlTimeScale(100, 0f);
        else          m_ScaleHandle.Release();
        
        HUD.Instance.GameplayUI.gameObject.SetActive(!NewPause); //Hide Gameplay UI
        
        Target.SetActive(NewPause);
        m_Paused        = NewPause;
        m_NextPauseTime = Time.unscaledTime + 0.5f; //This should be neccessary, but I dont have time to debug the Input Handling. Pressed should only be valid for a single frame
    }
}
