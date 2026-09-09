using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-100)]
public class SS_AudioSourceManager : MonoBehaviour
{
    public static SS_AudioSourceManager Instance;
    
    public List<AudioSource> AudioSources = new();
    public List<AudioSource> PlayingAudioSources = new();
    
    
    
    private void Awake()
    {
        Instance = this;
    }
}