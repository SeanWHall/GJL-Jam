using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioPlaySound : MonoBehaviour
{
    private AudioSource myAudioSource; // a private reference to an AudioSource variable
    public AudioClip mySound; // a public reference to an AudioClip (sound)

    // Start is called before the first frame update
    void Start()
    {
        myAudioSource = GetComponent<AudioSource>(); // finds the AudioSource component within the GameObject
    }

    // OnTriggerEnter is called when an actor enters the collider
    void PlaySound()
    {
        myAudioSource.clip = mySound;
        myAudioSource.Play();
    }
}