using UnityEngine;
using UnityEngine.Rendering;

public class Building : BaseBehaviour
{
    public MeshRenderer[] Renderers;
    
    public void OnTriggerEnter(Collider other)
    {
        if (!IsPlayer(other))
            return;

        SetRenderersState(false);
    }

    public void OnTriggerExit(Collider other)
    {
        if (!IsPlayer(other))
            return;

        SetRenderersState(true);
    }

    private bool IsPlayer(Collider Col) => Col.GetComponentInChildren<Player>() != null || Col.GetComponentInParent<Player>() != null;

    private void SetRenderersState(bool State)
    {
        foreach (var Renderer in Renderers)
            Renderer.shadowCastingMode = State ? ShadowCastingMode.On : ShadowCastingMode.Off;
    }
}
