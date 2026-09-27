using System.Collections.Generic;
using UnityEngine;

namespace AudioTrace
{
    [DefaultExecutionOrder(-100)]
    public class AudioSourceManager : MonoBehaviour
    {
        public static AudioSourceManager Instance;
    
        public List<AudioSource> AudioSources = new();
        public List<AudioSource> PlayingAudioSources = new();
    
    
    
        private void Awake()
        {
            Instance = this;
        }
    }
}