using UnityEngine;

public class KillZone : BaseBehaviour
{
    public void OnTriggerEnter(Collider other)
    {
        if (!Player.IsPartOfPlayer(other))
            return;
        
        //Make the player respawn at the last safe location, which will be the last visisted dock
        Player.Instance.Respawn();
    }

    private void OnDrawGizmos()
    {
        Collider Col = GetComponent<Collider>();
        if (Col == null)
            return;
        
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawWireCube(Vector3.zero, Vector3.one);
        Gizmos.color = new Color(1, 0, 0, 0.1f);
        Gizmos.DrawCube(Col.bounds.center, Col.bounds.extents);
    }
}
