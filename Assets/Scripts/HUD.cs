using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HUD : BaseBehaviour
{
    public static HUD Instance { get; private set; }
    
    public RawImage BlackScreen;

    private Coroutine m_Fade_Routine;

    public override void OnEnable()
    {
        base.OnEnable();

        BlackScreen.gameObject.SetActive(false);
        Instance = this;
    }

    public enum eFadeScreenEvent { Middle, End }

    public delegate void FadeScreenDel(eFadeScreenEvent Events);
    public void FadeScreen(float Speed, float TimeOnBlack, FadeScreenDel Del)
    {
        if (m_Fade_Routine != null)
        {
            StopCoroutine(m_Fade_Routine);
            m_Fade_Routine = null;
        }

        //Coroutines generate a ton of GC, but this aint the time to be worring about such things
        m_Fade_Routine = StartCoroutine(AsyncFadeScreen());
        IEnumerator AsyncFadeScreen()
        {
            BlackScreen.gameObject.SetActive(true);
            //Fade In
            yield return Fade(0f, 1f);
            
            Del?.Invoke(eFadeScreenEvent.Middle);
            yield return new WaitForSeconds(TimeOnBlack);
            
            //Fade Out
            yield return Fade(1f, 0f);
            Del?.Invoke(eFadeScreenEvent.End);
            BlackScreen.gameObject.SetActive(false);
        }

        IEnumerator Fade(float Start, float Target)
        {
            float Alpha = 0f;
            while (Alpha < 1f)
            {
                SetAlpha(BlackScreen, Mathf.Lerp(Start, Target, Alpha));
                yield return new WaitForEndOfFrame();
                Alpha += Speed * Time.deltaTime;
            }
            SetAlpha(BlackScreen, Target);
        }
    }

    public void SetAlpha(Graphic Target, float Alpha)
    {
        if (Target == null)
            return;

        Color color = Target.color;
        color.a      = Alpha;
        Target.color = color;
    }
}
