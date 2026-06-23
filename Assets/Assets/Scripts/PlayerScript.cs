using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider))]
public class PlayerScript : MonoBehaviour
{
    [Header("Move")]
    [SerializeField] private float walkSpeed = 5f;
    [SerializeField] private float sprintMultiplier = 1.8f;
    [SerializeField] private float crouchMultiplier = 0.6f;
    [SerializeField] private float acceleration = 40f; // units/sec^2
    [SerializeField] private float airControl = 0.5f;   // 0..1

    [Header("Jump / Gravity")]
    [SerializeField] private float jumpForce = 6f;
    [SerializeField] private float gravityMultiplier = 2.0f;

    [Header("Mouse Look")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private float mouseSensitivity = 100f;
    [SerializeField] private float verticalLookLimit = 85f;
    [SerializeField] private bool lockCursor = true;

    [Header("Ground")]
    [SerializeField] private Transform groundCheck; // a child transform near feet
    [SerializeField] private float groundCheckRadius = 0.2f;
    [SerializeField] private LayerMask groundLayers;
    [SerializeField] private string groundTag = "";
    [SerializeField] private float slopeLimit = 50f; // degrees

    // runtime
    private Rigidbody _rb;
    private CapsuleCollider _capsule;
    private float _yaw;
    private float _pitch;
    private Vector3 _input; // x=strafe, z=forward
    private bool _jumpRequested;
    private bool _isGrounded;
    private Vector3 _groundNormal = Vector3.up;

    void Start()
    {
        _rb = GetComponent<Rigidbody>();
        _capsule = GetComponent<CapsuleCollider>();

        if (_rb == null) Debug.LogError("FPSMovement: Rigidbody missing");
        if (_capsule == null) Debug.LogError("FPSMovement: CapsuleCollider missing");
        if (playerCamera == null) Debug.LogWarning("FPSMovement: Camera not assigned");

        // recommended Rigidbody settings
        _rb.interpolation = RigidbodyInterpolation.Interpolate;
        _rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        _rb.freezeRotation = true;

        if (lockCursor)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        _yaw = transform.eulerAngles.y;
    }

    void Update()
    {
        // Read mouse
        float mx = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float my = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;
        _yaw += mx;
        _pitch -= my;
        _pitch = Mathf.Clamp(_pitch, -verticalLookLimit, verticalLookLimit);

        if (playerCamera != null)
            playerCamera.transform.localEulerAngles = new Vector3(_pitch, 0f, 0f);

        // Read movement input (raw for snappy control)
        float hx = Input.GetAxisRaw("Horizontal");
        float vz = Input.GetAxisRaw("Vertical");
        Vector2 in2 = new Vector2(hx, vz);
        if (in2.sqrMagnitude > 1f) in2.Normalize();
        _input = new Vector3(in2.x, 0f, in2.y);

        // Jump
        if (Input.GetButtonDown("Jump"))
            _jumpRequested = true;
    }

    void FixedUpdate()
    {
        if (_rb == null) return;

        // Apply rotation in physics step for stable rotation
        Quaternion targetRot = Quaternion.Euler(0f, _yaw, 0f);
        _rb.MoveRotation(Quaternion.RotateTowards(_rb.rotation, targetRot, 1000f * Time.fixedDeltaTime));

        // Ground check (OverlapSphere more stable than single ray)
        PerformGroundCheck();

        // Determine desired speed (walk/sprint/crouch)
        bool sprint = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        bool crouch = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
        float speed = walkSpeed * (sprint ? sprintMultiplier : 1f) * (crouch ? crouchMultiplier : 1f);

        // Desired velocity in world space (aligned to body yaw)
        Vector3 desiredLocal = new Vector3(_input.x, 0f, _input.z) * speed;
        Vector3 desiredWorld = transform.rotation * desiredLocal;

        // If grounded and slope too steep, project desired onto slope plane to slide
        if (_isGrounded)
        {
            float slopeAngle = Vector3.Angle(_groundNormal, Vector3.up);
            if (slopeAngle > slopeLimit)
            {
                // slide down
                Vector3 slide = Vector3.ProjectOnPlane(Vector3.down, _groundNormal) * speed;
                desiredWorld = slide;
            }
            else
            {
                // project movement onto ground plane so character hugs slopes
                desiredWorld = Vector3.ProjectOnPlane(desiredWorld, _groundNormal);
            }
        }

        // Current horizontal velocity
        Vector3 current = _rb.linearVelocity;
        Vector3 currentHorizontal = new Vector3(current.x, 0f, current.z);

        // Choose control strength (air control reduces responsiveness)
        float control = _isGrounded ? 1f : Mathf.Clamp01(airControl);

        // Move current toward target using acceleration scaled by control
        float maxDelta = acceleration * control * Time.fixedDeltaTime;
        Vector3 newHorizontal = Vector3.MoveTowards(currentHorizontal, desiredWorld, maxDelta);

        // Apply velocity while preserving vertical component (gravity handled by physics)
        _rb.linearVelocity = new Vector3(newHorizontal.x, current.y, newHorizontal.z);

        // Jump impulse
        if (_jumpRequested)
        {
            if (_isGrounded)
            {
                // reset vertical velocity then apply impulse for consistent jump height
                Vector3 vel = _rb.linearVelocity;
                vel.y = 0f;
                _rb.linearVelocity = vel;
                _rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            }
            _jumpRequested = false;
        }

        // Extra gravity when falling for snappy feel
        if (_rb.linearVelocity.y < -0.01f)
        {
            _rb.AddForce(Physics.gravity * (gravityMultiplier - 1f), ForceMode.Acceleration);
        }
    }

    private void PerformGroundCheck()
    {
        _isGrounded = false;
        _groundNormal = Vector3.up;

        if (groundCheck == null)
        {
            // fallback: compute origin near feet
            if (_capsule != null)
            {
                Vector3 origin = _capsule.bounds.center + Vector3.down * (_capsule.bounds.extents.y - 0.01f);
                Collider[] hits = Physics.OverlapSphere(origin, groundCheckRadius, groundLayers, QueryTriggerInteraction.Ignore);
                foreach (var c in hits)
                {
                    if (c == null || c == _capsule) continue;
                    if (string.IsNullOrEmpty(groundTag) || c.CompareTag(groundTag))
                    {
                        _isGrounded = true;
                        // get normal from closest hit via ray
                        if (Physics.Raycast(origin, Vector3.down, out RaycastHit rh, groundCheckRadius + 0.5f, groundLayers, QueryTriggerInteraction.Ignore))
                            _groundNormal = rh.normal;
                        break;
                    }
                }
            }
            return;
        }

        // Preferred: overlap at groundCheck position
        Collider[] hits2 = Physics.OverlapSphere(groundCheck.position, groundCheckRadius, groundLayers, QueryTriggerInteraction.Ignore);
        if (hits2 != null && hits2.Length > 0)
        {
            foreach (var c in hits2)
            {
                if (c == null || c == _capsule) continue;
                if (string.IsNullOrEmpty(groundTag) || c.CompareTag(groundTag))
                {
                    _isGrounded = true;
                    // ray down to sample normal
                    if (Physics.Raycast(groundCheck.position + Vector3.up * 0.1f, Vector3.down, out RaycastHit rh2, 0.5f, groundLayers, QueryTriggerInteraction.Ignore))
                        _groundNormal = rh2.normal;
                    break;
                }
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        else if (_capsule != null)
            Gizmos.DrawWireSphere(transform.position + Vector3.down * (_capsule.height * 0.5f - _capsule.radius), groundCheckRadius);
    }
}