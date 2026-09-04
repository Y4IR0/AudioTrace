using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class AcousticListener : MonoBehaviour
{
    private class AcousticRay
    {
        public List<Vector3> Points = new();
        public float Distance;
    }
    
    
    
    [SerializeField] private int raysCount = 100;
    [SerializeField] private int bouncesCount = 8;
    [SerializeField] private float maxStepDistance = 50;
    [SerializeField] private int verticalLevels = 5;
    [SerializeField] private float verticalSpread = 0.7f;

    private List<AudioSource> sources = new();
    
    private List<AcousticRay> allRays = new();
    private List<float> distances = new();

    
    
    
    
    
    
    
    private void Awake()
    {
        sources.AddRange(FindObjectsByType<AudioSource>());
        RayTraceAudioSource(sources[0]);
    }

    private void RayTraceAudioSource(AudioSource source)
    {
        Vector3 origin = source.transform.position;
        
        int raysPerLevel = Mathf.Max(1, raysCount / verticalLevels);

        for (int level = 0; level < verticalLevels; level++)
        {
            float height = Mathf.Lerp(-1f, 1f, (float)level / (verticalLevels - 1));

            float horizontalSpread = 1f - Mathf.Abs(height) * verticalSpread;



            for (int i = 0; i < raysPerLevel; i++)
            {
                float angle = (float)i / raysPerLevel * 360f;

                float x = Mathf.Cos(angle * Mathf.Deg2Rad) * horizontalSpread;
                float z = Mathf.Sin(angle * Mathf.Deg2Rad) * horizontalSpread;
                
                Vector3 direction = new Vector3(x, height, z).normalized;
                
                TraceRay(new Ray(origin, direction));
            }
        }

        float averageDistance = distances.Count > 0 ? distances.Average() : 0;
        Debug.Log(distances.Count);
        Debug.Log(averageDistance);
    }

    private void TraceRay(Ray ray)
    {
        AcousticRay acousticRay = new();
        acousticRay.Points.Add(ray.origin);
        
        float totalDistance = 0f;

        for (int bounce = -1; bounce < bouncesCount; bounce++)
        {
            if (Physics.Raycast(ray, out RaycastHit hit, maxStepDistance))
            {
                totalDistance += hit.distance;
                acousticRay.Points.Add(hit.point);
                
                // Check if hit listener
                if (hit.collider.gameObject.name == "AcousticCollider")
                {
                    acousticRay.Distance = totalDistance;
                    RegisterHit(totalDistance);
                    break;
                }
                
                // Bounce
                ray.direction = Vector3.Reflect(ray.direction, hit.normal);
                
                // Move slightly
                ray.origin = hit.point + ray.direction * 0.01f;
            }
            else
            {
                acousticRay.Points.Add(ray.origin + ray.direction * maxStepDistance);
                break;
            }
        }
        
        allRays.Add(acousticRay);
    }

    private void RegisterHit(float distance)
    {
        distances.Add(distance);
    }

    private void OnDrawGizmos()
    {
        foreach (AcousticRay acousticRay in allRays)
        {
            for (int i = 1; i < acousticRay.Points.Count; i++)
            {
                Gizmos.DrawLine(acousticRay.Points[i - 1], acousticRay.Points[i]);
            }
        }
    }
}
