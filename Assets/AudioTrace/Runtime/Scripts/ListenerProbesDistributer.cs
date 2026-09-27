using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AudioTrace
{
    public class ListenerProbesDistributer : MonoBehaviour
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
            public Vector3 Normal;
            public bool Hit;
        }

        private class DirectAudioSourceOcclusion
        {
            public readonly AudioSource AudioSource;
            public readonly Collider Collider;

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
            public readonly AudioSource AudioSource;
            public ProbeData Probe;
            public float Length;
            public readonly List<Vector3> Points;

            public Path(
                AudioSource audioSource,
                ProbeData probe,
                List<Vector3> points)
            {
                AudioSource = audioSource;
                Probe = probe;
                Points = points;

                Length = Vector3.Distance(points[1], points[0]) + probe.Distance;
            }
        }


        
        
        
        
        
        
        
        
        
        
        
        // ===================================== Variables =====================================
        [Header("Probe Settings")] [SerializeField]
        private int probeCount = 200;

        [SerializeField] private int probeLayers = 10;
        [Range(0f, 1f)] [SerializeField] private float probeUpdatePercentage = 0.1f;
        [SerializeField] private float maxProbeDistance = 35f;
        [SerializeField] private float probeHeight = 1.5f;
        [Range(0f, 1f)] [SerializeField] private float verticalImportance = 0.4f;

        private ProbeData[] _probes;
        private Vector3[] _probeDirections;
        private int _probeIndex;


        [Header("Direct AudioSource Settings")] [SerializeField]
        private Collider listenerCollider;

        private readonly List<DirectAudioSourceOcclusion> _directAudioSourceOcclusions = new();


        [Header("Path Settings")] private readonly List<Path> _paths = new();


        [Header("Occlusion Settings")] [SerializeField]
        private float occlusionWallMultiplier = 1f;

        [SerializeField] private float maxOcclusionWallThickness = 2f;
        [SerializeField] private float occlusionPathMultiplier = 0.3f;
        [SerializeField] private float occlusionNoPathMultiplier = 2f;
        [SerializeField] private float minOcclusionCutoff = 500f;
        [SerializeField] private float maxOcclusionCutoff = 22000f;


        [Header("Audio Settings")] [SerializeField]
        private float roomSizeDistance = 20f;

        [SerializeField] private float audioSourceSmoothTime = 0.08f;

        private readonly Dictionary<AudioSource, AudioSource> _perceivedAudioSources = new();
        private readonly Dictionary<AudioSource, AudioLowPassFilter> _audioLowPassFilters = new();
        private readonly Dictionary<AudioSource, AudioReverbFilter> _audioReverbFilters = new();

        private readonly Dictionary<AudioSource, float> _occlusions = new();
        private float _reverbIntensity = 0f;
        private float _roomSize = 0f;


        [Header("Debugging")] [SerializeField] private bool showProbes;
        [SerializeField] private bool showWallOcclusions;
        [SerializeField] private bool showPaths;
        [SerializeField] private bool showDebugCanvases;
        [SerializeField] private AudioSourceDebugCanvas debugCanvasPrefab;

        private readonly Dictionary<AudioSource, AudioSourceDebugCanvas> _debugCanvases = new();
        private List<Vector3> _wallOcclusionPoints = new();


        
        
        
        
        
        
        
        
        
        
        
        
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

            GenerateProbeDirections();
        }

        private void Update()
        {
            if (AudioSourceManager.Instance == null)
                return;

            UpdateProbes();
            CheckDirectAudioSourceOcclusions();
            PathOccludedAudioSources();
            UpdateOcclusions();
            UpdateReverbs();
            UpdatePerceivedAudioSources();
        }

        private void GenerateProbeDirections()
        {
            _probeDirections = new Vector3[probeCount];

            if (probeCount <= 0)
                return;

            int probesPerLayer = probeCount / probeLayers;

            for (int layer = 0; layer < probeLayers; layer++)
            {
                float y = probeLayers == 1 ? -1f : Mathf.Lerp(-1f, 1f, (float)layer / (probeLayers - 1));
                y *= (1f - verticalImportance); // Apply verticalImportance

                for (int i = 0; i < probesPerLayer; i++)
                {
                    float angle = 2f * Mathf.PI * i / probesPerLayer;
                    float x = Mathf.Cos(angle);
                    float z = Mathf.Sin(angle);

                    Vector3 position = new Vector3(x, y, z);
                    Vector3 direction = position.normalized;

                    _probeDirections[i + (probesPerLayer * layer)] = direction;
                }
            }
        }

        private void UpdateProbes()
        {
            Vector3 origin = transform.position;
            int probesPerFrame = Mathf.CeilToInt(probeCount * probeUpdatePercentage);

            for (int i = 0; i < probesPerFrame; i++)
            {
                Ray ray = new Ray(origin, _probeDirections[_probeIndex]);

                // Update probe
                if (Physics.Raycast(ray, out RaycastHit hit, maxProbeDistance))
                {
                    _probes[_probeIndex] = new ProbeData
                    {
                        Position = hit.point,
                        Distance = hit.distance,
                        Normal = hit.normal,
                        Hit = true
                    };
                }
                else
                {
                    _probes[_probeIndex] = new ProbeData
                    {
                        Position = origin + ray.direction * maxProbeDistance,
                        Distance = maxProbeDistance,
                        Normal = Vector3.zero,
                        Hit = false
                    };
                }

                _probeIndex++;

                if (_probeIndex >= probeCount)
                {
                    _probeIndex = 0;
                }
            }
        }

        private void CheckDirectAudioSourceOcclusions()
        {
            _directAudioSourceOcclusions.Clear();

            foreach (AudioSource audioSource in AudioSourceManager.Instance.PlayingAudioSources)
            {
                if (!IsAudioSourceInRange(audioSource))
                    continue;
                
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
                //ProbeData[] sortedProbes = GetSortedProbes(origin); // Disabled for performance

                for (int i = 0; i < _probes.Length; i++)
                {
                    ProbeData probe = _probes[i];

                    Ray ray = new Ray(origin, probe.Position - origin);
                    if (!Physics.Raycast(ray, out RaycastHit hit, maxDistance))
                        continue;

                    if (hit.point != probe.Position)
                        continue;

                    index = i;
                    break;
                }

                // Path found
                if (index != -1)
                {
                    ProbeData pathedProbe = _probes[index];
                    List<Vector3> points = new();

                    points.Add(origin);
                    points.Add(pathedProbe.Position);
                    points.Add(transform.position);

                    Path path = new Path(audioSource, pathedProbe, points);
                    _paths.Add(path);
                }
            }
        }

        private void UpdateOcclusions()
        {
            _occlusions.Clear();

            foreach (AudioSource audioSource in AudioSourceManager.Instance.PlayingAudioSources)
            {
                if (!IsAudioSourceInRange(audioSource))
                    continue;
                
                // Check if in line of sight
                DirectAudioSourceOcclusion directAudioSourceOcclusion =
                    _directAudioSourceOcclusions.Find(x => x.AudioSource == audioSource);

                if (directAudioSourceOcclusion != null && directAudioSourceOcclusion.Collider == listenerCollider)
                {
                    _occlusions.Add(audioSource, 0f);
                    continue;
                }

                float wallWidthOcclusion =
                    GetWallsOcclusion(audioSource.transform.position, transform.position);
                float distance = Vector3.Distance(audioSource.transform.position, transform.position);
                float distanceFactor = 0f;

                // Calculate path occlusion
                Path path = _paths.Find(x => x.AudioSource == audioSource);

                if (path != null)
                {
                    distanceFactor = distance / audioSource.maxDistance * occlusionPathMultiplier;

                    float occlusion = wallWidthOcclusion / 5 + distanceFactor;
                    occlusion = Mathf.Clamp01(occlusion);

                    _occlusions.Add(audioSource, occlusion);
                    continue;
                }

                // No path found
                distanceFactor = distance / audioSource.maxDistance * occlusionNoPathMultiplier;
                wallWidthOcclusion *= occlusionNoPathMultiplier;

                float totalOcclusion = wallWidthOcclusion + distanceFactor;
                totalOcclusion = Mathf.Clamp01(totalOcclusion);

                _occlusions.Add(audioSource, totalOcclusion);
            }
        }

        private void UpdateReverbs()
        {
            // Reverb Intensity
            float normalScore = 0f;
            int hitCount = 0;

            for (int i = 0; i < _probes.Length; i++)
            {
                ProbeData probe = _probes[i];

                if (probe == null || !probe.Hit) // Check if hit
                    continue;

                Vector3 direction = _probeDirections[i];
                float directness = Vector3.Dot(probe.Normal, -direction);
                directness = Mathf.Pow(directness, 0.4f);
                directness = Mathf.Clamp01(directness);

                normalScore += directness;
                hitCount++;
            }

            if (hitCount > 0)
                normalScore /= hitCount;

            float outsideMultiplier = (float)hitCount / (float)_probes.Length; // Less reverb when near exit
            outsideMultiplier = Mathf.Pow(outsideMultiplier, 4f);
            normalScore *= outsideMultiplier;

            _reverbIntensity = Mathf.Clamp01(normalScore);


            // Room Size
            List<ProbeData> sortedProbes = _probes.ToList();
            sortedProbes.Sort((a, b) => b.Distance.CompareTo(a.Distance)); // Disabled for performance, disabled is ~0.4ms faster

            float totalDistance = 0;
            int useCount = (int)(sortedProbes.Count * 0.6f);

            for (int i = 0; i < useCount; i++)
            {
                ProbeData probe = sortedProbes[i];
                totalDistance += probe.Distance;
            }

            float averageDistance = (totalDistance / useCount) / roomSizeDistance;
            averageDistance = Mathf.Clamp01(averageDistance);

            _roomSize = averageDistance;
        }

        private void UpdatePerceivedAudioSources()
        {
            // Create perceivedAudioSources
            foreach (AudioSource audioSource in AudioSourceManager.Instance.PlayingAudioSources)
            {
                if (_perceivedAudioSources.TryGetValue(audioSource,
                        out AudioSource perceivedAudioSource)) // Check if already exists
                    continue;


                GameObject cloneObject = new GameObject($"{audioSource.name}_Indirect");
                perceivedAudioSource = cloneObject.AddComponent<AudioSource>();

                SetDefaultAudioSourceSettings(audioSource, perceivedAudioSource);
                perceivedAudioSource.transform.position = audioSource.transform.position;

                _perceivedAudioSources.Add(audioSource, perceivedAudioSource);


                // Occlusion
                AudioLowPassFilter cloneAudioLowPassFilter = cloneObject.AddComponent<AudioLowPassFilter>();
                if (audioSource.gameObject.TryGetComponent<AudioLowPassFilter>(
                        out AudioLowPassFilter originalAudioLowPassFilter))
                    SetDefaultAudioLowPassFilterSettings(cloneAudioLowPassFilter, originalAudioLowPassFilter,
                        audioSource);
                else
                    SetDefaultAudioLowPassFilterSettings(cloneAudioLowPassFilter, audioSource);

                _audioLowPassFilters.Add(audioSource, cloneAudioLowPassFilter);


                // Reverb
                AudioReverbFilter cloneAudioReverbFilter = cloneObject.AddComponent<AudioReverbFilter>();
                if (audioSource.gameObject.TryGetComponent<AudioReverbFilter>(
                        out AudioReverbFilter originalAudioReverbFilter))
                    SetDefaultAudioReverbFilterSettings(cloneAudioReverbFilter, originalAudioReverbFilter);
                else
                    SetDefaultAudioReverbFilterSettings(cloneAudioReverbFilter);

                _audioReverbFilters.Add(audioSource, cloneAudioReverbFilter);


                // Prevent overlapping
                audioSource.mute = true;
                perceivedAudioSource.Play();
            }

            // Destroy unused perceivedAudioSources
            List<AudioSource> perceivedAudioSources = new List<AudioSource>(_perceivedAudioSources.Keys);

            foreach (AudioSource audioSource in perceivedAudioSources)
            {
                if (AudioSourceManager.Instance.PlayingAudioSources
                    .Contains(audioSource)) // Check if audioSource is playing
                    continue;

                // Remove perceivedAudioSource
                AudioSource perceivedAudioSource = _perceivedAudioSources[audioSource];
                Destroy(perceivedAudioSource.gameObject);

                _perceivedAudioSources.Remove(audioSource);
                _audioLowPassFilters.Remove(audioSource);
                _audioReverbFilters.Remove(audioSource);

                // Remove transition velocities
                foreach (var key in _floatTransitionVelocities.Keys.ToList())
                {
                    if (key.Item1 == audioSource)
                        _floatTransitionVelocities.Remove(key);
                }

                foreach (var key in _vectorTransitionVelocities.Keys.ToList())
                {
                    if (key.Item1 == audioSource)
                        _vectorTransitionVelocities.Remove(key);
                }

                // Restore original audioSource
                audioSource.mute = false;
            }

            // Sync time
            foreach (AudioSource audioSource in _perceivedAudioSources.Keys)
            {
                AudioSource perceivedAudioSource = _perceivedAudioSources[audioSource];
                float margin = 0.05f;

                if (Mathf.Abs(perceivedAudioSource.time - audioSource.time) < margin) // Check if synced
                    continue;

                perceivedAudioSource.time = audioSource.time;
            }

            // Update position
            foreach (AudioSource audioSource in _perceivedAudioSources.Keys)
            {
                AudioSource perceivedAudioSource = _perceivedAudioSources[audioSource];

                Vector3 targetPosition = Vector3.zero;

                Path connectedPath = null;
                foreach (Path path in _paths)
                {
                    if (path.AudioSource == audioSource)
                        connectedPath = path;
                }

                if (connectedPath != null)
                {
                    Vector3 point0 = connectedPath.Points[0];
                    Vector3 point1 = connectedPath.Points[1];
                    Vector3 midpoint = Vector3.Lerp(point0, point1, 0.5f);
                    Vector3 direction = (point1 - point0).normalized * 2;
                    targetPosition = midpoint + direction;
                }
                else
                    targetPosition = audioSource.transform.position;


                // Transition position
                perceivedAudioSource.transform.position = Transition(audioSource, "position",
                    perceivedAudioSource.transform.position, targetPosition);
            }

            // Update components
            foreach (AudioSource audioSource in _perceivedAudioSources.Keys)
            {
                AudioSource perceivedAudioSource = _perceivedAudioSources[audioSource];

                UpdateAudioSourceSettings(audioSource, perceivedAudioSource);
                UpdateAudioLowPassFilterSettings(audioSource, _audioLowPassFilters[audioSource]);
                UpdateAudioReverbFilterSettings(audioSource, _audioReverbFilters[audioSource]);
            }
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

        private float GetWallsOcclusion(Vector3 origin, Vector3 end)
        {
            Vector3 direction = end - origin;
            float distance = direction.magnitude;

            // Forward
            RaycastHit[] forwardHits = Physics.RaycastAll(origin, direction.normalized, distance);

            // Reverse
            RaycastHit[] reverseHits = Physics.RaycastAll(end, -direction.normalized, distance);


            if (forwardHits.Length == 0 || reverseHits.Length == 0) // No hits
                return 0f;


            // Sort based on distance from origins
            forwardHits = forwardHits.OrderBy(hit => hit.distance).ToArray();
            reverseHits = reverseHits.OrderBy(hit => hit.distance).ToArray();

            int wallCount = Mathf.Min(forwardHits.Length, reverseHits.Length);

            float totalWidth = 0f;

            for (int i = 0; i < wallCount; i++)
            {
                Vector3 entrancePoint = forwardHits[i].point;
                Vector3 exitPoint = reverseHits[reverseHits.Length - 1 - i].point;

                float width = Vector3.Distance(entrancePoint, exitPoint);

                totalWidth += width;

                // Debugging
                _wallOcclusionPoints.Add(entrancePoint);
                _wallOcclusionPoints.Add(exitPoint);
            }

            float occlusion =
                Mathf.Clamp(totalWidth, 0f, maxOcclusionWallThickness) / maxOcclusionWallThickness; // 0-1 clamp

            occlusion *= occlusionWallMultiplier;

            return occlusion;
        }

        private bool IsAudioSourceInRange(AudioSource audioSource)
        {
            float distance = (audioSource.transform.position - transform.position).sqrMagnitude;
            float maxDistance = audioSource.maxDistance;
            
            return distance <= maxDistance * maxDistance;
        }

        private float GetDistanceVolume(AudioSource original)
        {
            float distance = Vector3.Distance(original.transform.position, transform.position);

            if (distance <= original.minDistance)
                return 1f;
            
            float t = Mathf.InverseLerp(original.minDistance, original.maxDistance, distance);

            switch (original.rolloffMode)
            {
                case AudioRolloffMode.Logarithmic:
                    return original.minDistance / distance;
                
                case AudioRolloffMode.Linear:
                    return 1f - t;
                
                case AudioRolloffMode.Custom:
                    return original.GetCustomCurve(AudioSourceCurveType.CustomRolloff).Evaluate(t);
                
                default:
                    return 1f;
            }
        }

        private void SetDefaultAudioSourceSettings(AudioSource original, AudioSource perceivedAudioSource)
        {
            // Apply settings (can't find any other method)
            perceivedAudioSource.clip = original.clip;
            perceivedAudioSource.outputAudioMixerGroup = original.outputAudioMixerGroup;
            perceivedAudioSource.playOnAwake = original.playOnAwake;
            perceivedAudioSource.loop = original.loop;

            perceivedAudioSource.volume = Transition(original, "volume", perceivedAudioSource.volume, original.volume * GetDistanceVolume(original));
            perceivedAudioSource.pitch = original.pitch;
            perceivedAudioSource.priority = original.priority;

            perceivedAudioSource.spatialBlend = original.spatialBlend;
            perceivedAudioSource.panStereo = original.panStereo;
            perceivedAudioSource.spatialize = original.spatialize;
            perceivedAudioSource.spatializePostEffects = original.spatializePostEffects;
            perceivedAudioSource.dopplerLevel =
                0f; //original.dopplerLevel; Having a doppler level creates audio bugs due to the movement of the perceived audio source.
            perceivedAudioSource.spread = original.spread;

            perceivedAudioSource.rolloffMode = original.rolloffMode;
            perceivedAudioSource.minDistance = original.minDistance;
            perceivedAudioSource.maxDistance = original.maxDistance;

            perceivedAudioSource.reverbZoneMix = original.reverbZoneMix;
            perceivedAudioSource.ignoreListenerVolume = original.ignoreListenerVolume;
            perceivedAudioSource.ignoreListenerPause = original.ignoreListenerPause;

            // Curves
            perceivedAudioSource.SetCustomCurve(
                AudioSourceCurveType.CustomRolloff,
                original.GetCustomCurve(AudioSourceCurveType.CustomRolloff)
            );

            perceivedAudioSource.SetCustomCurve(
                AudioSourceCurveType.ReverbZoneMix,
                original.GetCustomCurve(AudioSourceCurveType.ReverbZoneMix)
            );

            perceivedAudioSource.SetCustomCurve(
                AudioSourceCurveType.SpatialBlend,
                original.GetCustomCurve(AudioSourceCurveType.SpatialBlend)
            );

            perceivedAudioSource.SetCustomCurve(
                AudioSourceCurveType.Spread,
                original.GetCustomCurve(AudioSourceCurveType.Spread)
            );
        }

        private void UpdateAudioSourceSettings(AudioSource original, AudioSource perceivedAudioSource)
        {
            if (perceivedAudioSource.clip != original.clip)
            {
                perceivedAudioSource.clip = original.clip;
                perceivedAudioSource.Play();
            }

            perceivedAudioSource.outputAudioMixerGroup = original.outputAudioMixerGroup;
            perceivedAudioSource.playOnAwake = original.playOnAwake;
            perceivedAudioSource.loop = original.loop;

            perceivedAudioSource.volume = Transition(original, "volume", perceivedAudioSource.volume, original.volume * GetDistanceVolume(original));
            perceivedAudioSource.pitch = original.pitch;
            perceivedAudioSource.priority = original.priority;

            perceivedAudioSource.spatialBlend = original.spatialBlend;
            perceivedAudioSource.panStereo = original.panStereo;
            perceivedAudioSource.spatialize = original.spatialize;
            perceivedAudioSource.spatializePostEffects = original.spatializePostEffects;
            //perceivedAudioSource.dopplerLevel = 0f; //original.dopplerLevel; Having a doppler level creates audio bugs due to the movement of the perceived audio source.
            perceivedAudioSource.spread = original.spread;

            perceivedAudioSource.rolloffMode = original.rolloffMode;
            perceivedAudioSource.minDistance = original.minDistance;
            perceivedAudioSource.maxDistance = original.maxDistance;

            perceivedAudioSource.reverbZoneMix = original.reverbZoneMix;
            perceivedAudioSource.ignoreListenerVolume = original.ignoreListenerVolume;
            perceivedAudioSource.ignoreListenerPause = original.ignoreListenerPause;
        }

        // Occlusion
        private void SetDefaultAudioLowPassFilterSettings(AudioLowPassFilter original, AudioLowPassFilter clone,
            AudioSource audioSource)
        {
            clone.lowpassResonanceQ = original.lowpassResonanceQ;

            float occlusion = _occlusions.GetValueOrDefault(audioSource, 0f);
            float targetCutoffFrequency = Mathf.Lerp(maxOcclusionCutoff, minOcclusionCutoff, occlusion);

            clone.cutoffFrequency =
                Transition(audioSource, "cutoffFrequency", clone.cutoffFrequency, targetCutoffFrequency);
        }

        private void SetDefaultAudioLowPassFilterSettings(AudioLowPassFilter clone, AudioSource audioSource)
        {
            clone.lowpassResonanceQ = 1; // No effect

            float occlusion = _occlusions.GetValueOrDefault(audioSource, 0f);
            float targetCutoffFrequency = Mathf.Lerp(maxOcclusionCutoff, minOcclusionCutoff, occlusion);

            clone.cutoffFrequency =
                Transition(audioSource, "cutoffFrequency", clone.cutoffFrequency, targetCutoffFrequency);
        }

        private void UpdateAudioLowPassFilterSettings(AudioSource audioSource, AudioLowPassFilter clone)
        {
            float occlusion = _occlusions.GetValueOrDefault(audioSource, 0f);
            float targetCutoffFrequency = Mathf.Lerp(maxOcclusionCutoff, minOcclusionCutoff, occlusion);

            clone.cutoffFrequency =
                Transition(audioSource, "cutoffFrequency", clone.cutoffFrequency, targetCutoffFrequency);
        }

        // Reverb
        private void SetDefaultAudioReverbFilterSettings(AudioReverbFilter original, AudioReverbFilter clone)
        {
            clone.reverbPreset = original.reverbPreset;

            clone.room = Mathf.Lerp(-4000, 0f, _roomSize);
            clone.decayTime = Mathf.Lerp(0.8f, 4f, _roomSize);
            clone.reflectionsLevel = Mathf.Lerp(-6000, 20f, _reverbIntensity);
            clone.reflectionsDelay = Mathf.Lerp(0f, 0.3f, _roomSize);
            clone.reverbLevel = Mathf.Lerp(-3000, 180f, _reverbIntensity);
            clone.reverbDelay = Mathf.Lerp(0f, 0.1f, _roomSize);
        }

        private void SetDefaultAudioReverbFilterSettings(AudioReverbFilter clone)
        {
            clone.dryLevel = 0f;

            clone.diffusion = 100f;
            clone.density = 100f;

            clone.room = Mathf.Lerp(-4000, 0f, _roomSize);
            clone.decayTime = Mathf.Lerp(0.8f, 4f, _roomSize);
            clone.reflectionsLevel = Mathf.Lerp(-6000, 20f, _reverbIntensity);
            clone.reflectionsDelay = Mathf.Lerp(0f, 0.3f, _roomSize);
            clone.reverbLevel = Mathf.Lerp(-3000, 180f, _reverbIntensity);
            clone.reverbDelay = Mathf.Lerp(0f, 0.1f, _roomSize);
        }

        private void UpdateAudioReverbFilterSettings(AudioSource audioSource, AudioReverbFilter clone)
        {
            clone.room = Transition(audioSource, "room", clone.room, Mathf.Lerp(-4000, 0f, _roomSize));
            clone.decayTime = Transition(audioSource, "decayTime", clone.decayTime, Mathf.Lerp(0.8f, 4f, _roomSize));
            clone.reflectionsLevel = Transition(audioSource, "reflectionsLevel", clone.reflectionsLevel,
                Mathf.Lerp(-6000, 20f, _reverbIntensity));
            clone.reflectionsDelay = Transition(audioSource, "reflectionsDelay", clone.reflectionsDelay,
                Mathf.Lerp(0f, 0.3f, _roomSize));
            clone.reverbLevel = Transition(audioSource, "reverbLevel", clone.reverbLevel,
                Mathf.Lerp(-3000, 180f, _reverbIntensity));
            clone.reverbDelay = Transition(audioSource, "reverbDelay", clone.reverbDelay,
                Mathf.Lerp(0f, 0.1f, _roomSize));
        }


        private readonly Dictionary<(AudioSource, string), float> _floatTransitionVelocities = new();
        private readonly Dictionary<(AudioSource, string), Vector3> _vectorTransitionVelocities = new();

        private float Transition(AudioSource audioSource, string id, float current, float target)
        {
            var key = (audioSource, id);

            float velocity = _floatTransitionVelocities.GetValueOrDefault(key, 0f);
            float result = Mathf.SmoothDamp(current, target, ref velocity, audioSourceSmoothTime);

            _floatTransitionVelocities[key] = velocity;
            return result;
        }

        private Vector3 Transition(AudioSource audioSource, string id, Vector3 current, Vector3 target)
        {
            var key = (audioSource, id);

            Vector3 velocity = _vectorTransitionVelocities.GetValueOrDefault(key, Vector3.zero);
            Vector3 result = Vector3.SmoothDamp(current, target, ref velocity, audioSourceSmoothTime);

            _vectorTransitionVelocities[key] = velocity;
            return result;
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

            // showWallOcclusions
            if (showWallOcclusions)
            {
                if (_wallOcclusionPoints == null) return;

                Gizmos.color = Color.darkRed;

                foreach (Vector3 wallOcclusionPoint in _wallOcclusionPoints)
                {
                    Gizmos.DrawSphere(wallOcclusionPoint, 0.05f);
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

            // Clear
            _wallOcclusionPoints.Clear();
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
            List<AudioSource> audioSources = AudioSourceManager.Instance.PlayingAudioSources;

            // Create
            foreach (AudioSource audioSource in audioSources)
            {
                // Check if exists
                if (_debugCanvases.ContainsKey(audioSource))
                    continue;

                // Instantiate
                AudioSourceDebugCanvas debugCanvas = Instantiate(debugCanvasPrefab);
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
            foreach (AudioSource audioSource in AudioSourceManager.Instance.PlayingAudioSources)
            {
                if (!_debugCanvases.TryGetValue(audioSource, out AudioSourceDebugCanvas debugCanvas))
                    continue;

                float occlusion = _occlusions.GetValueOrDefault(audioSource, 0f);

                debugCanvas.UpdateInfo(
                    occlusion,
                    _reverbIntensity,
                    _roomSize
                );
            }
        }
    }
}