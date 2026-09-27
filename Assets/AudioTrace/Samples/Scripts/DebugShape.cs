using UnityEngine;

namespace AudioTrace.Demo
{
    public class DebugShape : MonoBehaviour
    {
        private enum Shape
        {
            Sphere,
            Cube,
        }
    
        [SerializeField] private Shape shape = Shape.Sphere;
        [SerializeField] private Vector3 size = Vector3.one;
        [SerializeField] private float radius = 1f;
        [SerializeField] private Color color = Color.red;
        [SerializeField] private bool isFilled;

        private void OnDrawGizmos()
        {
            Gizmos.color = color;
            Gizmos.matrix = transform.localToWorldMatrix;
        
            switch (shape)
            {
                case Shape.Sphere:
                    if (isFilled)
                        Gizmos.DrawSphere(Vector3.zero, radius);
                    else
                        Gizmos.DrawWireSphere(Vector3.zero, radius);
                    break;
            
                case Shape.Cube:
                    if (isFilled)
                        Gizmos.DrawCube(Vector3.zero, size);
                    else
                        Gizmos.DrawWireCube(Vector3.zero, size);
                    break;
            }
        }
    }

}