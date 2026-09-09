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
        SS_AudioSourceManager.Instance.AudioSources.Add(audioSource);
    }

    private void Update()
    {
        // Update playingAudioSources
        bool isPlaying = audioSource.isPlaying;

        if (wasPlaying == isPlaying) return;

        switch (isPlaying)
        {
            case true:
                SS_AudioSourceManager.Instance.PlayingAudioSources.Add(audioSource);
                break;
            
            case false:
                SS_AudioSourceManager.Instance.PlayingAudioSources.Remove(audioSource);
                break;
        }
        
        wasPlaying = isPlaying;
    }
}