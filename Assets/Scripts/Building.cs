using UnityEngine;
using UnityEngine.Rendering;

public class Building : BaseBehaviour
{
    public MeshRenderer[] Renderers;
    
    public void OnTriggerEnter(Collider other)
    {
        if (!IsPlayer(other) && !IsBoat(other))
            return;

        SetRenderersState(false);

        if (CameraController.Instance != null)
        {
            Debug.Log("Switch Camera State to Player");
            CameraController.Instance.ActiveState = CameraController.Instance.PlayerState;
        }
    }

    public void OnTriggerExit(Collider other)
    {
        if (!IsPlayer(other) && !IsBoat(other))
            return;

        SetRenderersState(true);
        if (CameraController.Instance != null)
        {
            Debug.Log("Switch Camera State to Boat");
            CameraController.Instance.ActiveState = CameraController.Instance.BoatState;
        }
    }

    private bool IsPlayer(Collider Col) => Col.GetComponentInChildren<Player>() != null || Col.GetComponentInParent<Player>() != null;
    private bool IsBoat(Collider Col)   => Col.GetComponentInChildren<Boat>() != null || Col.GetComponentInParent<Boat>() != null;

    private void SetRenderersState(bool State)
    {
        foreach (var Renderer in Renderers)
            Renderer.shadowCastingMode = State ? ShadowCastingMode.On : ShadowCastingMode.ShadowsOnly;
    }
}
