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

    [Tooltip("Only allow jumping when the collider under the player has this tag. Leave empty to allow any collider as ground.")]

    [SerializeField] private string groundTag = "";

    [Header("Health")]

    [SerializeField] private int maxHealth = 3;

    private Rigidbody _rb;

    private Collider _col;

    private Vector3 _moveInput;

    private bool _jumpRequested;

    private bool _isGrounded;

    private bool _canJump;

    private float _pitch; // vertical look

    private int _currentHealth;

    private bool _isDead;

    void Start()

    {

        _rb = GetComponent<Rigidbody>();

        _col = GetComponent<Collider>();

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

        _currentHealth = Mathf.Max(1, maxHealth);

        _isDead = false;

        // initialize jump availability based on initial grounded state

        UpdateGroundStateImmediate();

        _canJump = _isGrounded;

    }

    void Update()

    {

        if (_isDead)

            return;

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

        // Read input and normalize to avoid faster diagonal movement

        float rawH = Input.GetAxis("Horizontal");

        float rawV = Input.GetAxis("Vertical");

        Vector2 input2 = new Vector2(rawH, rawV);

        if (input2.sqrMagnitude > 1f)

            input2.Normalize();

        _moveInput = new Vector3(input2.x, 0f, input2.y);

        // update grounded state each frame

        UpdateGroundStateImmediate();

        // If grounded, allow the next jump

        if (_isGrounded)

            _canJump = true;

        // Jump request - only when grounded and only registers on GetButtonDown and only if allowed (_canJump)

        if (Input.GetButtonDown("Jump") && _isGrounded && _canJump)

        {

            _jumpRequested = true;

            _canJump = false; // prevent further jumps until we touch ground again

        }

    }

    void FixedUpdate()

    {

        if (_isDead)

            return;

        // Convert input to world space based on player orientation

        Vector3 worldMove = transform.TransformDirection(_moveInput) * moveSpeed;

        // Preserve current Y velocity (gravity / jumping handled by physics)

        Vector3 velocity = _rb.linearVelocity;

        velocity.x = worldMove.x;

        velocity.z = worldMove.z;

        _rb.linearVelocity = velocity;

        // Handle jump in physics step (only once per request)

        if (_jumpRequested)

        {

            // Double-check grounded before applying impulse to avoid edge cases

            if (_isGrounded)

            {

                _rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);

            }

            _jumpRequested = false;

        }

    }

    // Performs a raycast + overlap checks and updates _isGrounded

    private void UpdateGroundStateImmediate()

    {

        // Determine check position (groundCheck or fallback just below collider)

        Vector3 checkPos;

        if (groundCheck != null)

        {

            checkPos = groundCheck.position;

        }

        else if (_col != null)

        {

            checkPos = transform.position + Vector3.down * (_col.bounds.extents.y + 0.05f);

        }

        else

        {

            checkPos = transform.position + Vector3.down * 0.6f;

        }

        // Determine mask to use

        int mask = (groundMask.value != 0) ? groundMask.value : Physics.DefaultRaycastLayers;

        _isGrounded = false;

        // Prefer a short raycast down

        float rayDist = (_col != null) ? (_col.bounds.extents.y + 0.1f) : 0.6f;

        if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, rayDist + groundCheckRadius, mask, QueryTriggerInteraction.Ignore))

        {

            if (string.IsNullOrEmpty(groundTag) || hit.collider.CompareTag(groundTag))

            {

                _isGrounded = true;

                return;

            }

        }

        // Fallback to overlap sphere

        Collider[] overlaps = Physics.OverlapSphere(checkPos, groundCheckRadius, mask, QueryTriggerInteraction.Ignore);

        if (overlaps != null && overlaps.Length > 0)

        {

            foreach (var c in overlaps)

            {

                if (c == null)

                    continue;

                if (c == _col)

                    continue;

                if (string.IsNullOrEmpty(groundTag) || c.CompareTag(groundTag))

                {

                    _isGrounded = true;

                    break;

                }

            }

        }

    }

    // Public API for health

    public void TakeDamage(int amount = 1)

    {

        if (_isDead)

            return;

        _currentHealth -= Mathf.Max(0, amount);

        _currentHealth = Mathf.Clamp(_currentHealth, 0, maxHealth);

        if (_currentHealth <= 0)

        {

            Die();

        }

    }

    private void Die()

    {

        if (_isDead)

            return;

        _isDead = true;

        // Stop physics motion and prevent further input

        if (_rb != null)

        {

            _rb.linearVelocity = Vector3.zero;

            _rb.isKinematic = true;

        }

        // unlock cursor so user can interact with UI after death

        Cursor.lockState = CursorLockMode.None;

        Cursor.visible = true;

        // disable this component so no further input is processed

        enabled = false;

    }

    // Optional: visualize ground check sphere in the editor

    void OnDrawGizmosSelected()

    {

        Vector3 checkPos;

        if (groundCheck != null)

        {

            checkPos = groundCheck.position;

        }

        else if (TryGetComponent<Collider>(out var c))

        {

            checkPos = transform.position + Vector3.down * (c.bounds.extents.y + 0.05f);

        }

        else

        {

            checkPos = transform.position + Vector3.down * 0.6f;

        }

        Gizmos.color = Color.yellow;

        Gizmos.DrawWireSphere(checkPos, groundCheckRadius);

    }

    // Read-only current health

    public int CurrentHealth => _currentHealth;

}

