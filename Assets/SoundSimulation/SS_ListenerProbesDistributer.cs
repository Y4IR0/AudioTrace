using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class SS_ListenerProbesDistributer : MonoBehaviour
{
    // ===================================== Classes =====================================
    private class RayData
    {
        public List<Vector3> Points = new();
        public float Distance;
    }

    private class ProbeData
    {
        public Vector3 Position;
        public float Distance;
    }
    
    private class DirectAudioSourceOcclusion
    {
        public AudioSource AudioSource;
        public Collider Collider;

        public DirectAudioSourceOcclusion(
            AudioSource audioSource,
            Collider collider)
        {
            AudioSource = audioSource;
            Collider = collider;
        }
    }

    private class Path
    {
        public AudioSource AudioSource;
        public ProbeData Probe;
        public List<Vector3> Points;
        
        public Path(
            AudioSource audioSource,
            ProbeData probe,
            List<Vector3> points)
        {
            AudioSource = audioSource;
            Probe = probe;
            Points = points;
        }
    }

    
    
    
    
    
    
    
    
    
    
    
    
    
    // ===================================== Variables =====================================
    [Header("Probe Settings")]
    [SerializeField] private int probeCount = 200;
    [SerializeField] private float maxProbeDistance = 35f;
    
    private ProbeData[] _probes;
    
    
    [Header("Direct AudioSource Settings")]
    [SerializeField] private Collider listenerCollider;
    
    private List<DirectAudioSourceOcclusion> _directAudioSourceOcclusions = new();
    
    
    [Header("Path Settings")]
    private List<Path> _paths = new();
    
    
    [Header("Audio Settings")]
    private Dictionary<AudioSource, AudioSource> _audioSourceClones = new();
    
    
    [Header("Debugging")]
    [SerializeField] private bool showProbes;
    [SerializeField] private bool showPaths;
    [SerializeField] private bool showDebugCanvases;
    [SerializeField] private SS_AudioSourceDebugCanvas debugCanvasPrefab;

    private Dictionary<AudioSource, SS_AudioSourceDebugCanvas> _debugCanvases = new();


    
    
    
    
    
    
    
    
    
    
    
    
    
    

    // ===================================== Functions =====================================
    private void Awake()
    {
        _probes = new ProbeData[probeCount];
        
        if (!listenerCollider) // If listenerCollider isn't filled in inspector
        {
            listenerCollider = GetComponent<Collider>();
            
            if (!listenerCollider)
                listenerCollider = gameObject.GetComponentInParent<Collider>();
        }
    }
    
    private void Update()
    {
        UpdateProbes();
        CheckDirectAudioSourceOcclusions();
        PathOccludedAudioSources();
    }

    private void UpdateProbes()
    {
        Vector3 origin = transform.position;
        
        for (int i = 0; i < probeCount; i++)
        {
            Ray ray = new Ray(origin, Random.onUnitSphere);

            // Update probe
            if (Physics.Raycast(ray, out RaycastHit hit, maxProbeDistance))
            {
                _probes[i] = new ProbeData
                {
                    Position = hit.point,
                    Distance = hit.distance
                };
            }
            else
            {
                _probes[i] = new ProbeData
                {
                    Position = origin + ray.direction * maxProbeDistance,
                    Distance = maxProbeDistance
                };
            }
        }
    }

    private void CheckDirectAudioSourceOcclusions()
    {
        _directAudioSourceOcclusions.Clear();
        
        foreach (AudioSource audioSource in SS_AudioSourceManager.Instance.PlayingAudioSources)
        {
            Vector3 origin = audioSource.gameObject.transform.position;

            // Checks if audio is in listener
            if (listenerCollider.bounds.Contains(origin))
            {
                _directAudioSourceOcclusions.Add(new DirectAudioSourceOcclusion(audioSource, listenerCollider));
                continue;
            }
            
            
            Vector3 direction = transform.position - origin;
            Ray ray = new Ray(origin, direction);
            
            // Checks if audio is in line of sight of listener
            if (Physics.Raycast(ray, out RaycastHit hit, direction.magnitude))
                _directAudioSourceOcclusions.Add(new DirectAudioSourceOcclusion(audioSource, hit.collider));
        }
    }

    private void PathOccludedAudioSources()
    {
        _paths.Clear();
        
        foreach (DirectAudioSourceOcclusion directAudioSourceOcclusion in _directAudioSourceOcclusions)
        {
            if (directAudioSourceOcclusion.Collider == listenerCollider)
                continue;
            
            AudioSource audioSource = directAudioSourceOcclusion.AudioSource;
            Vector3 origin = audioSource.transform.position;
            float maxDistance = audioSource.maxDistance;
            int index = -1;
            ProbeData[] sortedProbes = GetSortedProbes(origin);

            for (int i = 0; i < sortedProbes.Length; i++)
            {
                ProbeData probe = sortedProbes[i];
                
                Ray ray = new Ray(origin, probe.Position - origin);
                if (!Physics.Raycast(ray, out RaycastHit hit, maxDistance))
                    continue;
                
                if (hit.point != probe.Position)
                    continue;
                
                index = i;
                break;
            }

            if (index != -1)
            {
                ProbeData pathedProbe = sortedProbes[index];
                List<Vector3> points = new List<Vector3>();
                
                points.Add(origin);
                points.Add(pathedProbe.Position);
                points.Add(listenerCollider.transform.position);

                Path path = new Path(audioSource, pathedProbe, points);
                _paths.Add(path);
                
                CreateOrUpdateAudioSourceClone(path);
                
                Debug.Log($"Attempts: {index:D}");
            }
            else
            {
                Debug.LogWarning("No probes found!");
            }
        }
    }

    private void CreateOrUpdateAudioSourceClone(Path path)
    {
        AudioSource original = path.AudioSource;

        // Create clone if not existing yet
        if (!_audioSourceClones.TryGetValue(original, out AudioSource clone))
        {
            GameObject cloneObject = new GameObject($"{original.name}_Indirect");
            clone = cloneObject.AddComponent<AudioSource>();
            
            CopyAudioSourceSettings(original, clone);
            
            _audioSourceClones.Add(original, clone);
            
            // Prevent overlapping
            original.mute = true;
            clone.Play();
            clone.time = original.time;
        }
        
        // Position clone
        Vector3 point0 = path.Points[0];
        Vector3 point1 = path.Points[1];
        
        Vector3 midpoint = Vector3.Lerp(point0, point1, 0.5f);
        Vector3 direction = (point1 - point0).normalized;
        
        clone.transform.position = midpoint + direction;
    }
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    // ===================================== Helpers =====================================
    private ProbeData[] GetSortedProbes(Vector3 audioSourcePosition)
    {
        ProbeData[] sortedProbes = (ProbeData[])_probes.Clone();
        
        Array.Sort(sortedProbes, (a, b) =>
        {
            float distanceA = (a.Position - audioSourcePosition).sqrMagnitude;
            float distanceB = (b.Position - audioSourcePosition).sqrMagnitude;
            
            return distanceA.CompareTo(distanceB);
        });

        return sortedProbes;
    }

    private void CopyAudioSourceSettings(AudioSource original, AudioSource clone)
    {
        // Apply settings (can't find any other method)
        clone.clip = original.clip;
        clone.outputAudioMixerGroup = original.outputAudioMixerGroup;
        clone.playOnAwake = original.playOnAwake;
        clone.loop = original.loop;
            
        clone.volume = original.volume;
        clone.pitch = original.pitch;
        clone.priority = original.priority;
            
        clone.spatialBlend = original.spatialBlend;
        clone.panStereo = original.panStereo;
        clone.spatialize = original.spatialize;
        clone.spatializePostEffects = original.spatializePostEffects;
        clone.dopplerLevel = original.dopplerLevel;
        clone.spread = original.spread;
            
        clone.rolloffMode = original.rolloffMode;
        clone.minDistance = original.minDistance;
        clone.maxDistance = original.maxDistance;
            
        clone.reverbZoneMix = original.reverbZoneMix;
        clone.ignoreListenerVolume = original.ignoreListenerVolume;
        clone.ignoreListenerPause = original.ignoreListenerPause;
        
        // Curves
        clone.SetCustomCurve(
            AudioSourceCurveType.CustomRolloff,
            original.GetCustomCurve(AudioSourceCurveType.CustomRolloff)
            );
        
        clone.SetCustomCurve(
            AudioSourceCurveType.ReverbZoneMix,
            original.GetCustomCurve(AudioSourceCurveType.ReverbZoneMix)
        );
        
        clone.SetCustomCurve(
            AudioSourceCurveType.SpatialBlend,
            original.GetCustomCurve(AudioSourceCurveType.SpatialBlend)
        );
        
        clone.SetCustomCurve(
            AudioSourceCurveType.Spread,
            original.GetCustomCurve(AudioSourceCurveType.Spread)
        );
    }

    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    // ===================================== Debugging =====================================
    private void OnDrawGizmos()
    {
        // showProbes
        if (showProbes)
        {
            if (_probes == null) return;
        
            Gizmos.color = Color.deepSkyBlue;
        
            foreach (ProbeData probe in _probes)
            {
                if (probe == null) continue;
            
                Gizmos.DrawSphere(probe.Position, 0.1f);
            }
        }
        
        // showPaths
        if (showPaths)
        {
            if (_paths == null) return;
            
            Gizmos.color = Color.blue;

            foreach (Path path in _paths)
            {
                if (path == null) continue;

                for (int i = 0; i < path.Points.Count - 1; i++)
                {
                    Gizmos.DrawLine(
                        path.Points[i],
                        path.Points[i + 1]
                        );
                }
            }
        }
    }

    private void LateUpdate()
    {
        if (showDebugCanvases)
        {
            UpdateDebugCanvases();
            UpdateDebugCanvasInfos();
        }
    }

    private void UpdateDebugCanvases()
    {
        List<AudioSource> audioSources = SS_AudioSourceManager.Instance.AudioSources;

        // Create
        foreach (AudioSource audioSource in audioSources)
        {
            // Check if exists
            if (_debugCanvases.ContainsKey(audioSource))
                continue;
            
            // Instantiate
            SS_AudioSourceDebugCanvas debugCanvas = Instantiate(debugCanvasPrefab);
            debugCanvas.SetAudioSource(audioSource);
            debugCanvas.gameObject.SetActive(showDebugCanvases);
            
            _debugCanvases.Add(audioSource, debugCanvas);
        }

        // Destroy
        foreach (AudioSource audioSource in new List<AudioSource>(_debugCanvases.Keys))
        {
            if (!audioSources.Contains(audioSource))
            {
                Destroy(_debugCanvases[audioSource].gameObject);
                _debugCanvases.Remove(audioSource);
            }
        }
    }
    
    private void UpdateDebugCanvasInfos()
    {
        foreach (AudioSource audioSource in SS_AudioSourceManager.Instance.PlayingAudioSources)
        {
            if (!_debugCanvases.TryGetValue(audioSource, out SS_AudioSourceDebugCanvas debugCanvas))
                continue;
            
            debugCanvas.UpdateInfo(
                0.123123f,
                0.45645f
            );
        }
    }
}
