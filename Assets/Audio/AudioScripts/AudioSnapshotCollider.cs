using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class AudioSnapshotCollider : MonoBehaviour
{
    public AudioMixerSnapshot ColliderSnapshot;
    public float SnapshotTransitionTime;

    // OnTriggerEnter is called when an actor enters the collider
    void OnTriggerEnter(Collider other)
    {
        ColliderSnapshot.TransitionTo(SnapshotTransitionTime);
    }

}
