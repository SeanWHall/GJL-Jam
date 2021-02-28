using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InteractionTest : BaseBehaviour, IInteractable
{
    public DialogueAsset Asset;
    
    public float   InteractionDistance => 10f;
    public string  InteractionText     => "Test Interaction";
    public Vector3 Position            => transform.position;

    public Material[] InteractionMaterials { get; private set; }

    public void Awake() => InteractionMaterials = GetComponent<MeshRenderer>().materials;

    public bool CanInteract(Player player) => player.ActiveState == player.LocomotionState;

    public void OnInteract(Player player)
    {
        if (DialogueManager.IsInDialogue)
            return;
        
        DialogueManager.StartDialogue(Asset);
    }
}
