using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(PlayerInput))]
public class PlayerController : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float runMultiplier = 2.4f;
    [SerializeField] private float lookSensitivity = .1f;
    [SerializeField] private Transform cameraTransform;
    
    private Rigidbody rb;
    private PlayerInput playerInput;
    
    private Vector2 movementVector;
    private Vector2 lookVector;
    private bool isRunning;
    
    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        playerInput = GetComponent<PlayerInput>();
        
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationY | RigidbodyConstraints.FreezeRotationZ;
        
        Cursor.lockState = CursorLockMode.Locked;
    }
    
    void Update()
    {
        movementVector = playerInput.actions["Move"].ReadValue<Vector2>();
        lookVector = playerInput.actions["Look"].ReadValue<Vector2>();
        isRunning = playerInput.actions["Sprint"].IsPressed();

        RotateCamera();
    }
    
    void FixedUpdate()
    {
        Vector3 direction = transform.rotation * new Vector3(movementVector.x, 0, movementVector.y);
        
        float speed = isRunning ? moveSpeed * runMultiplier : moveSpeed;
        
        rb.linearVelocity = direction * speed;
    }

    void RotateCamera()
    {
        transform.Rotate(0, lookVector.x * lookSensitivity, 0);
        
        cameraTransform.localRotation *= Quaternion.Euler(-lookVector.y * lookSensitivity, 0, 0);
    }
}
