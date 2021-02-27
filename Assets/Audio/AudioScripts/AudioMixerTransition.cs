using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class AudioMixerTransition : MonoBehaviour
{
    public AudioMixerSnapshot nextSnapshot;
    public float transitionTime;

    // Start is called before the first frame update
    void Start()
    {
        nextSnapshot.TransitionTo(transitionTime);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
