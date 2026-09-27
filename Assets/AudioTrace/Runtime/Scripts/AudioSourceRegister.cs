using UnityEngine;

namespace AudioTrace
{
    [DefaultExecutionOrder(0)]
    public class AudioSourceRegister : MonoBehaviour
    {
        private AudioSource _audioSource;
        private bool _wasPlaying;

        private void Awake()
        {
            _audioSource = GetComponent<AudioSource>();

            if (AudioSourceManager.Instance == null)
            {
                enabled = false;
                return;
            }
        
            AudioSourceManager.Instance.AudioSources.Add(_audioSource);
        }

        private void Update()
        {
            // Update playingAudioSources
            bool isPlaying = _audioSource.isPlaying;

            if (_wasPlaying == isPlaying) return;

            switch (isPlaying)
            {
                case true:
                    AudioSourceManager.Instance.PlayingAudioSources.Add(_audioSource);
                    break;
            
                case false:
                    AudioSourceManager.Instance.PlayingAudioSources.Remove(_audioSource);
                    break;
            }
        
            _wasPlaying = isPlaying;
        }

        private void OnDisable()
        {
            AudioSourceManager.Instance?.PlayingAudioSources.Remove(_audioSource);
            _wasPlaying = false;
        }
    }
}