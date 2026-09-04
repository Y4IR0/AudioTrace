using System;
using UnityEngine;

[DefaultExecutionOrder(0)]
public class SS_AudioSourceRegister : MonoBehaviour
{
    private AudioSource audioSource;
    private bool wasPlaying = false;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        SS_AudioSourceManager.instance.audioSources.Add(audioSource);
    }

    private void Update()
    {
        // Update playingAudioSources
        bool isPlaying = audioSource.isPlaying;

        if (wasPlaying == isPlaying) return;

        switch (isPlaying)
        {
            case true:
                SS_AudioSourceManager.instance.playingAudioSources.Add(audioSource);
                break;
            
            case false:
                SS_AudioSourceManager.instance.playingAudioSources.Remove(audioSource);
                break;
        }
        
        wasPlaying = isPlaying;
    }
}