using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InteractionTest : BaseBehaviour, IInteractable
{
    public float   InteractionDistance => 10f;
    public string  InteractionText     => "Test Interaction";
    public Vector3 Position            => transform.position;

    public Material[] InteractionMaterials { get; private set; }

    public void Awake() => InteractionMaterials = GetComponent<MeshRenderer>().materials;

    public bool CanInteract(Player player) => true; //TEst

    public void OnInteract(Player player)
    {
        Debug.Log("Interact with player");
    }
}
