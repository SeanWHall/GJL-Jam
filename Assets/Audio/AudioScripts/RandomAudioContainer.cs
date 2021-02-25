// using System.Collections;
// using System.Collections.Generic;
using UnityEngine.EventSystems;
using UnityEngine;
using System;
using Random = UnityEngine.Random; // TB - This line required for random range to work?

public class RandomAudioContainer : MonoBehaviour
{
    [Header("Audio Clip Array")]
    [Space(10)]
    public AudioClip[] FootstepSoundClips; // TB - Footstep sound file array
    [Header("Audio Source")]
    [Space(10)]
    public AudioSource AudioSourceGameObject; // TB - The audio source where the footstep sounds should play back from.

    [Header("Volume Randomisation")]
    [Space(10)]
    public float VolumeMin;
    public float VolumeMax;
    [Header("Pitch Randomisation")]
    [Space(10)]
    public float PitchMin;
    public float PitchMax;

    private int PickedFootstep;

    public void footstepWalkRight()
    {
        // Sound Randomisation whilst avoiding repetition

        AudioSourceGameObject.pitch = Random.Range(PitchMin, PitchMax);
        AudioSourceGameObject.volume = Random.Range(PitchMin, PitchMax);
        PickedFootstep = RepeatCheck(PickedFootstep, FootstepSoundClips.Length);
        AudioSourceGameObject.PlayOneShot(FootstepSoundClips[PickedFootstep]);
        Debug.Log("Right footstep walk: " + PickedFootstep + AudioSourceGameObject.volume + AudioSourceGameObject.pitch);
    }

    public void footstepWalkLeft()
    {

        // Sound Randomisation whilst avoiding repetition

        AudioSourceGameObject.pitch = Random.Range(PitchMin, PitchMax);
        AudioSourceGameObject.volume = Random.Range(PitchMin, PitchMax);
        PickedFootstep = RepeatCheck(PickedFootstep, FootstepSoundClips.Length);
        AudioSourceGameObject.PlayOneShot(FootstepSoundClips[PickedFootstep]);
        Debug.Log("Left footstep walk: " + PickedFootstep + AudioSourceGameObject.volume + AudioSourceGameObject.pitch);
    }
    int RepeatCheck(int previousIndex, int range) // This section checks whether a clip has already been selected, and chooses another if so.
    {
        int index = Random.Range(0, range);

        while (index == previousIndex)
        {
            index = Random.Range(0, range);
        }
        return index;
    }

}