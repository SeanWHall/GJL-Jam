using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class AudioMixerStartingSnapshot : MonoBehaviour
{
    public AudioMixerSnapshot startingSnapshot;
    public float TransitionTime;

    // Start is called before the first frame update
    void Start()
    {
        startingSnapshot.TransitionTo(TransitionTime);
    }
}
