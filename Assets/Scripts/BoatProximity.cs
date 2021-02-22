using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoatProximity : BaseBehaviour
{
    public static bool IsPlayerClose;
    
    public void OnTriggerEnter(Collider other)
    {
        if (Player.Instance == null || other.gameObject != Player.Instance.gameObject)
            return;

        IsPlayerClose = true;
    }
    
    public void OnTriggerExit(Collider other)
    {
        if (Player.Instance == null || other.gameObject != Player.Instance.gameObject)
            return;

        IsPlayerClose = false;
    }
}
