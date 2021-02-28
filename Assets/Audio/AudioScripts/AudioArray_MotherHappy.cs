using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioArray_MotherHappy : MonoBehaviour
{
    public AudioClip[] mySounds;
    public AudioSource myAudioSource;

    void PlayRandomSound()
    {
        // pick & play a random sound from the array - exclude index 0
        int n = Random.Range(1, mySounds.Length); // generate a random number between 1 and length of array
        if (mySounds[n]) // checks there is a sound at that index
        {
            myAudioSource.clip = mySounds[n]; // assigns chosen sound to AudioSource
            myAudioSource.Play(); // plays the sound
                                  // move sounds around within the array to do 'random without repeat'
            mySounds[n] = mySounds[0]; // move the sound currently at index 0 to the chosen index
            mySounds[0] = myAudioSource.clip; // move the chosen sound to index 0
        }
    }
}
