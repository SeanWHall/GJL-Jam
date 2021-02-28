using System.Collections;
using UnityEngine;
using UnityEngine.Playables;

public class TriggerDirector : BaseBehaviour
{
    public PlayableDirector Director;

    private bool Triggered;
    
    public void OnTriggerEnter(Collider other)
    {
        if (Triggered || !IsPlayer(other))
            return;
        
        Triggered = true;

        StartCoroutine(WaitTillFinishRoutine());
    }

    private IEnumerator WaitTillFinishRoutine()
    {
        while (HUD.IsFading)
            yield return new WaitForEndOfFrame();
        
        Player.Instance.gameObject.SetActive(false);
        
        Director.Play();
        while (Director.state == PlayState.Playing)
            yield return new WaitForEndOfFrame();
        
        HUD.Instance.FadeScreen(1.5f, 0.5f, OnFadeScreenEvent);
    }

    private void OnFadeScreenEvent(HUD.eFadeScreenEvent Event)
    {
        if(Event == HUD.eFadeScreenEvent.Middle)
            LoadingManager.LoadLevel("EndScreen");
    }
    
    private bool IsPlayer(Collider Col) => Col.GetComponentInChildren<Player>() != null || Col.GetComponentInParent<Player>() != null;
}
