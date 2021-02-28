using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

[System.Serializable]
public class DialogueOptionUI
{
    public GameObject Root;
    public Button     Button;
    public Text       Text;
}

public class HUD : BaseBehaviour
{
    public static HUD Instance { get; private set; }
    
    public override eUpdateFlags UpdateFlags => eUpdateFlags.RequireUpdate;

    [Header("General")]
    public RectTransform RootRect;
    public RectTransform GameplayUI;
    public RawImage      BlackScreen;
    
    [Header("Interaction UI")]
    public RectTransform InteractableRoot;
    public Text          InteractableText;

    [Header("Dialogue")] 
    public RectTransform      Dialogue_Root;
    public Text               Dialogue_Text;
    public DialogueOptionUI[] Dialogue_OptionButtons;
    
    private Camera        m_Camera; //TODO: Update this camera
    private IInteractable m_Interactable;
    private Coroutine     m_Fade_Routine;
    private Shader        m_Toony_Shader;
    private int           m_Dialogue_OptionsVisible;

    private Camera Camera
    {
        get
        {
            if (m_Camera == null)
                m_Camera = CameraController.Instance != null ? CameraController.Instance.Camera : Camera.main;
            return m_Camera;
        }
    }

    public override void OnEnable()
    {
        base.OnEnable();

        m_Toony_Shader = Shader.Find("Toony");
        HideDialogue();
        
        RootRect = (RectTransform) transform;
        BlackScreen.gameObject.SetActive(false);
        Instance = this;
    }

    public Vector2 WorldPointToCanvasPoint(Vector3 WorldPoint)
    {
        Vector2 CanvasSize = RootRect.sizeDelta;
        Vector2 ViewPos    =Camera.WorldToViewportPoint(WorldPoint);
        return new Vector2(ViewPos.x * CanvasSize.x - CanvasSize.x * 0.5f, ViewPos.y * CanvasSize.y - CanvasSize.y * 0.5f);
    }

    public override void OnUpdate(float DeltaTime)
    {
        base.OnUpdate(DeltaTime);

        //Move Interactable UI, to follow the interaction transform
        if (m_Interactable != null)
        {
            InteractableRoot.anchoredPosition = WorldPointToCanvasPoint(m_Interactable.Position);
            InteractableText.text             = m_Interactable.InteractionText; //TODO: This creates a fair bit of GC, but its fine for this jam
        }
    }

    public void HideDialogue()
    {
        ClearDialogueOptions();
        Dialogue_Root.gameObject.SetActive(false);
    }
    
    public void ShowDialogue()
    {
        Dialogue_Root.gameObject.SetActive(true);
    }

    public void ClearDialogueOptions()
    {
        int Len = Dialogue_OptionButtons.Length;
        for (int i = 0; i < Len; i++)
        {
            Dialogue_OptionButtons[i].Root.SetActive(false);
            Dialogue_OptionButtons[i].Button.onClick.RemoveAllListeners();
        }

        m_Dialogue_OptionsVisible = 0;
    }
    
    public void AddDialogueOption(string Text, UnityAction Listener)
    {
        if (m_Dialogue_OptionsVisible >= Dialogue_OptionButtons.Length)
        {
            Error("Failed to Add new Dialogue Option, as we have ran out of options");
            return;
        }

        //We are using pre-created UI Elements, to save on GC & Performance spent recreating the Canvas
        DialogueOptionUI Option = Dialogue_OptionButtons[m_Dialogue_OptionsVisible++];
        Option.Root.SetActive(true); //todo: Could do with some fading, doesn't look to good everything just appearing.. Polish though
        Option.Text.text =  Text; //Text text text... Doesn't read well
        Option.Button.onClick.AddListener(Listener);
    }

    public void HideInteractionUI()
    {
        if (m_Interactable == null)
            return; //We should already be hidden, probably should assert this
        
        SetupInteractionMaterials(false);
        
        InteractableRoot.gameObject.SetActive(false);
        m_Interactable = null;
    }

    public void ShowInteractionUI(IInteractable Target)
    {
        if (m_Interactable == Target || Target == null)
            return; //Dont do anything if we are already showing this interaction
        
        if(m_Interactable != null)
            SetupInteractionMaterials(false);
        
        m_Interactable        = Target;
        InteractableText.text = Target.InteractionText;
        InteractableRoot.gameObject.SetActive(true);

        SetupInteractionMaterials(true);
    }

    private void SetupInteractionMaterials(bool ShowRim)
    {
        if (m_Interactable == null)
            return;
        
        Material[] Interaction_Materials = m_Interactable.InteractionMaterials;
        if (Interaction_Materials == null)
            return;

        int Materials_Len = Interaction_Materials.Length;
        for (int i = 0; i < Materials_Len; i++)
        {
            Material Mat = Interaction_Materials[i];
            if(Mat.shader != m_Toony_Shader)
                continue; //Skip non toony Shaders
            
            if(ShowRim) Mat.EnableKeyword("_RIM_HIGHLIGHT");
            else        Mat.DisableKeyword("_RIM_HIGHLIGHT");
        }
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
