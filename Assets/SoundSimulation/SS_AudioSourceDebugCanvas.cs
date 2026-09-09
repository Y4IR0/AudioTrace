using TMPro;
using UnityEngine;

public class SS_AudioSourceDebugCanvas : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private Vector3 offset = Vector3.up * 1.5f;
    
    [Header("References")]
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text audioClipText;
    [SerializeField] private TMP_Text occlusionText;
    [SerializeField] private TMP_Text reverbText;
    
    private AudioSource _audioSource;

    
    
    public void SetAudioSource(AudioSource audioSource)
    {
        this._audioSource = audioSource;
    }
    
    public void UpdateInfo(float occlusion, float reverb)
    {
        // Update position
        transform.position = _audioSource.transform.position + offset;
        
        // Update texts
        nameText.text = $"Name: {_audioSource.name}";
        audioClipText.text = $"Clip: {(_audioSource.clip ? _audioSource.clip.name : "None")}";
        occlusionText.text = $"Occlusion: {occlusion:F2}";
        reverbText.text = $"Reverb: {reverb:F2}";
        
        // Update rotation
        Vector3 direction = Camera.main.transform.position - transform.position;
        direction.y = 0f;
        transform.rotation = Quaternion.LookRotation(direction) * Quaternion.Euler(0, 180, 0);
    }
}