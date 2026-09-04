using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-100)]
public class SS_AudioSourceManager : MonoBehaviour
{
    public static SS_AudioSourceManager instance;
    
    public List<AudioSource> audioSources = new();
    public List<AudioSource> playingAudioSources = new();
    
    
    
    private void Awake()
    {
        instance = this;
    }
}