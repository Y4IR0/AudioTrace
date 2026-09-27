using UnityEngine;

public class TransformMove : MonoBehaviour
{
    [SerializeField] private Transform[] points = new Transform[0];
    [SerializeField] private float speed = 2f;

    private int _currentPoint = 0;
    private int _direction = 1;

    void Start()
    {
        if (points.Length > 0) // Start at first point position
            transform.position = points[0].position;
    }
    
    void Update()
    {
        if (points.Length < 2) // Check if multiple points
            return;
        
        int nextPoint = _currentPoint + _direction;
        
        transform.position = Vector3.MoveTowards(
            transform.position, points[nextPoint].position, speed * Time.deltaTime
            );

        if (transform.position == points[nextPoint].position)
        {
            _currentPoint = nextPoint;
            
            // Reverse direction at last
            if (_currentPoint == points.Length - 1)
                _direction = -1;
            else if (_currentPoint == 0)
                _direction = 1;
        }
    }
}
