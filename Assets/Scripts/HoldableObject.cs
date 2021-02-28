using System;
using System.Collections.Generic;
using UnityEngine;

public class HoldableObject : BaseBehaviour, IInteractable
{
    public float      InteractionDistance  => PickupDistance;
    public string     InteractionText      => PickupText;
    public Vector3    Position             => transform.position;
    public Material[] InteractionMaterials { get; set; }
    
    public Transform LeftHand;
    public Transform RightHand;
    public Vector3   HoldOffset;
    public Vector3   HoldRotation;
    public string    PickupText;
    public float     PickupDistance = 5f;

    public override void OnEnable()
    {
        base.OnEnable();

        List<Material> Materials     = new List<Material>(); //TODO: Could do this so much cleaner
        MeshRenderer[] Renderers     = GetComponentsInChildren<MeshRenderer>();
        int            Renderers_Len = Renderers.Length;
        
        for(int i = 0; i < Renderers_Len; i++)
            Materials.AddRange(Renderers[i].materials);

        InteractionMaterials = Materials.ToArray();
    }

    public bool CanInteract(Player player) => player.ActiveState == player.LocomotionState; //Can only be picked up if player is moving around

    public void OnInteract(Player player)
    {
        transform.SetParent(player.PickupMountPoint);
        transform.localPosition = HoldOffset;
        transform.localRotation = Quaternion.Euler(HoldRotation);
        
        player.HoldingObjectState.HoldableObject = this;
        player.ActiveState = player.HoldingObjectState;
    }
}
