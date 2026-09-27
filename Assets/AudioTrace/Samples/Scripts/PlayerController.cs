using UnityEngine;
using UnityEngine.InputSystem;

namespace AudioTrace.Demo
{
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(PlayerInput))]
    public class PlayerController : MonoBehaviour
    {
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float runMultiplier = 2.4f;
        [SerializeField] private float lookSensitivity = .1f;
        [SerializeField] private Transform cameraTransform;
    
        private Rigidbody _rb;
        private PlayerInput _playerInput;
    
        private Vector2 _movementVector;
        private Vector2 _ookVector;
        private bool _isRunning;
    
        void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _playerInput = GetComponent<PlayerInput>();
        
            _rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationY | RigidbodyConstraints.FreezeRotationZ;
        
            Cursor.lockState = CursorLockMode.Locked;
        }
    
        void Update()
        {
            _movementVector = _playerInput.actions["Move"].ReadValue<Vector2>();
            _ookVector = _playerInput.actions["Look"].ReadValue<Vector2>();
            _isRunning = _playerInput.actions["Sprint"].IsPressed();

            RotateCamera();
        }
    
        void FixedUpdate()
        {
            Vector3 direction = transform.rotation * new Vector3(_movementVector.x, 0, _movementVector.y);
        
            float speed = _isRunning ? moveSpeed * runMultiplier : moveSpeed;
        
            _rb.linearVelocity = direction * speed;
        }

        void RotateCamera()
        {
            transform.Rotate(0, _ookVector.x * lookSensitivity, 0);
        
            cameraTransform.localRotation *= Quaternion.Euler(-_ookVector.y * lookSensitivity, 0, 0);
        }
    }
}