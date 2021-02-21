using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PauseMenu : BaseBehaviour
{
    public static PauseMenu Instance { get; private set; }
    public static bool      IsPaused => Instance != null && Instance.m_Paused;

    private bool m_Paused;
    
}
