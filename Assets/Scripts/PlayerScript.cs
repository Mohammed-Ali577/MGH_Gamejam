using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(Collider))]
public class PlayerScript : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float jumpForce = 5f;

    [Header("Mouse Look")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private float mouseSensitivity = 100f;
    [SerializeField] private float verticalLookLimit = 85f;
    [SerializeField] private bool invertY = false;
    [SerializeField] private bool lockCursor = true;

    [Header("Ground Check")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.2f;
    [SerializeField] private LayerMask groundMask;

    private Rigidbody _rb;
    private Vector3 _moveInput;
    private bool _jumpRequested;
    private bool _isGrounded;
    private float _pitch; // vertical look

    void Start()
    {
        _rb = GetComponent<Rigidbody>();
        // Use physics to move the player and avoid tipping over
        _rb.interpolation = RigidbodyInterpolation.Interpolate;
        _rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        _rb.freezeRotation = true;

        if (playerCamera == null)
        {
            playerCamera = GetComponentInChildren<Camera>();
        }

        if (lockCursor)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    void Update()
    {
        // Mouse look
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime * (invertY ? 1f : -1f);

        // Yaw - rotate the player body
        transform.Rotate(Vector3.up * mouseX);

        // Pitch - rotate camera up/down and clamp
        _pitch += mouseY;
        _pitch = Mathf.Clamp(_pitch, -verticalLookLimit, verticalLookLimit);
        if (playerCamera != null)
        {
            playerCamera.transform.localEulerAngles = new Vector3(-_pitch, 0f, 0f);
        }

        // Read input (WASD or arrow keys)
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        _moveInput = new Vector3(horizontal, 0f, vertical);

        // Ground check (uses groundCheck transform if assigned)
        Vector3 checkPos = groundCheck != null ? groundCheck.position : transform.position;
        _isGrounded = Physics.CheckSphere(checkPos, groundCheckRadius, groundMask, QueryTriggerInteraction.Ignore);

        // Jump request
        if (Input.GetButtonDown("Jump") && _isGrounded)
        {
            _jumpRequested = true;
        }
    }

    void FixedUpdate()
    {
        // Convert input to world space based on player orientation
        Vector3 worldMove = transform.TransformDirection(_moveInput) * moveSpeed;

        // Preserve current Y velocity (gravity / jumping handled by physics)
        Vector3 velocity = _rb.linearVelocity;
        velocity.x = worldMove.x;
        velocity.z = worldMove.z;
        _rb.linearVelocity = velocity;

        // Handle jump in physics step
        if (_jumpRequested)
        {
            _rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            _jumpRequested = false;
        }
    }

    // Optional: visualize ground check sphere in the editor
    void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
    }
}
