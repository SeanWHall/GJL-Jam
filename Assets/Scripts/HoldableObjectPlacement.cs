using UnityEngine;

public class HoldableObjectPlacement : BaseBehaviour, IInteractable
{
    public int        InteractionPriority  => 1;
    public float      InteractionDistance  => PlaceDistance;
    public string     InteractionText      => PlaceText;
    public Vector3    Position             => transform.position;
    public Material[] InteractionMaterials => null; //TODO: Would be cool to draw a rim around nothing

    public float          PlaceDistance = 5f;
    public string         PlaceText;
    public HoldableObject Target;
    public Transform      Player_Teleport;

    public bool CanInteract(Player player) => player.ActiveState is PlayerHoldingObjectState && player.HoldingObjectState.HoldableObject == Target;

    public void OnInteract(Player player)
    {
        player.ActiveState = player.LocomotionState;

        //Check if we should teleport the player
        if (Player_Teleport != null)
        {
            player.Controller.enabled            = false;
            player.Controller.transform.position = Player_Teleport.position;
            player.Controller.enabled            = false;
        }
        
        Target.transform.parent   = null;
        Target.transform.position = transform.position;
        Target.transform.rotation = transform.rotation;
    }

    private void OnDrawGizmos()
    {
        if (Target == null)
            return;

        MeshFilter[] Filters     = Target.GetComponentsInChildren<MeshFilter>();
        int          Filters_Len = Filters.Length;

        Gizmos.color = new Color(0, 0, 1, 0.25f);
        for (int i = 0; i < Filters_Len; i++)
        {
            MeshFilter Filter      = Filters[i];
            Mesh       Filter_Mesh = Filter.sharedMesh;
            if(Filter_Mesh == null)
                continue;

            Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, Filter.transform.localScale);
            Gizmos.DrawMesh(Filter_Mesh);
        }
    }
}
