using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Bat : MonoBehaviour
{
    [Header("Movement")]
    public float speed = 4f;
    public float turnSpeed = 3f;
    [Tooltip("Distance from home center bats will roam within.")]
    public float wanderRadius = 6f;
    [Tooltip("Min/max flight height relative to home center Y.")]
    public Vector2 heightRange = new Vector2(1f, 4f);
    [Tooltip("Seconds between forced target changes.")]
    public float changeTargetInterval = 3f;
    [Tooltip("Distance to target at which a new target is chosen.")]
    public float targetReachThreshold = 0.75f;

    [Header("Constraints")]
    [Tooltip("Minimum world Y the bats should stay above.")]
    public float minFlightHeight = 1f;

    [Header("Audio / Wing Flap")]
    public AudioClip wingFlapClip;
    [Tooltip("Volume for PlayOneShot")]
    [Range(0f, 1f)]
    public float wingVolume = 0.7f;
    [Tooltip("Interval range (seconds) between wing flap sounds")]
    public Vector2 flapIntervalRange = new Vector2(0.12f, 0.28f);
    [Tooltip("Random pitch variance applied to the audio source")]
    public Vector2 pitchRange = new Vector2(0.95f, 1.05f);

    [Header("Animation")]
    [Tooltip("Optional: Animator on the bat or a child. Animator should expose a float parameter named 'Speed' to control flying.")]
    public Animator animator;

    private Rigidbody rb;
    private Vector3 homeCenter;
    private Vector3 target;
    private float changeTimer;

    // Audio
    private AudioSource audioSource;
    private float flapTimer;

    // Animator hashes (-1 means unavailable)
    private int speedHash = -1;

    // Optional: call from spawner to set where this bat should consider "home"
    public void Initialize(Vector3 home)
    {
        homeCenter = home;
        // make sure home center respects min flight height so relative heights are above constraint
        homeCenter.y = Mathf.Max(homeCenter.y, minFlightHeight);
        PickNewTarget();
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.constraints = RigidbodyConstraints.FreezeRotation; // we control rotation

        // Ensure an AudioSource exists
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 1f; // 3D sound
        }

        // initialize flap timer
        flapTimer = Random.Range(flapIntervalRange.x, flapIntervalRange.y);

        // Animator: if not assigned in inspector, try to find one on this object or children
        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (animator != null)
        {
            // keep animator in sync with physics updates for smoother motion-driven animation
            animator.updateMode = AnimatorUpdateMode.Fixed;
            animator.applyRootMotion = false;

            // Only use Speed hash if the animator actually exposes that parameter
            if (AnimatorHasParameter(animator, "Speed"))
            {
                speedHash = Animator.StringToHash("Speed");
            }
            else
            {
                speedHash = -1;
#if UNITY_EDITOR
                Debug.LogWarning($"Bat: Animator on '{gameObject.name}' does not have a 'Speed' parameter. Animation parameter updates will be skipped.");
#else
                Debug.Log($"Bat: Animator missing 'Speed' parameter on {gameObject.name}.");
#endif
            }

            if (animator.runtimeAnimatorController == null)
            {
#if UNITY_EDITOR
                Debug.LogWarning($"Bat: Animator on '{gameObject.name}' has no RuntimeAnimatorController assigned.");
#else
                Debug.Log($"Bat: Animator missing controller on {gameObject.name}.");
#endif
            }
        }
    }

    private bool AnimatorHasParameter(Animator anim, string paramName)
    {
        if (anim == null) return false;
        foreach (var p in anim.parameters)
        {
            if (p.name == paramName) return true;
        }
        return false;
    }

    private void Start()
    {
        // If spawner didn't initialize home, use current position as center.
        if (homeCenter == Vector3.zero)
            homeCenter = transform.position;

        // ensure home center meets minimum height constraint
        homeCenter.y = Mathf.Max(homeCenter.y, minFlightHeight);

        // ensure current position is not below min height
        if (transform.position.y < minFlightHeight)
        {
            Vector3 p = transform.position;
            p.y = minFlightHeight;
            transform.position = p;
        }

        PickNewTarget();

        // Attempt to play a fallback clip if the controller contains a matching clip name.
        // Prefer common names used by the editor helper or your assets.
        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (animator != null && animator.runtimeAnimatorController != null)
        {
            var clips = animator.runtimeAnimatorController.animationClips;
            // accept any of these common names if present
            string[] preferredNames = new[] { "Fast_Flying", "Bat_Fly", "Fly", "Bat_Flying", "Bat_Fly_Loop" };
            string found = null;
            foreach (var clip in clips)
            {
                if (clip == null) continue;
                foreach (var n in preferredNames)
                {
                    if (clip.name == n)
                    {
                        found = clip.name;
                        break;
                    }
                }
                if (found != null) break;
            }

            if (found != null)
            {
                // Play on base layer (0).
                animator.Play(found, 0, 0f);
            }
            else
            {
                // If Speed parameter exists, we will drive transitions via SetFloat; otherwise log state.
                if (speedHash == -1)
                {
#if UNITY_EDITOR
                    Debug.LogWarning($"Bat: Animator on '{gameObject.name}' has controller but no preferred clip found and no 'Speed' parameter. Assign a controller with a 'Speed' parameter or a named clip/state.");
#else
                    Debug.Log($"Bat: Animator missing setup on {gameObject.name}.");
#endif
                }
            }
        }
    }

    private void Update()
    {
        changeTimer += Time.deltaTime;
        if (changeTimer >= changeTargetInterval)
        {
            PickNewTarget();
        }

        // Wing flap audio timing
        if (wingFlapClip != null && audioSource != null)
        {
            flapTimer -= Time.deltaTime;
            if (flapTimer <= 0f)
            {
                audioSource.pitch = Random.Range(pitchRange.x, pitchRange.y);
                audioSource.PlayOneShot(wingFlapClip, wingVolume);
                flapTimer = Random.Range(flapIntervalRange.x, flapIntervalRange.y);
            }
        }
    }

    private void FixedUpdate()
    {
        // Wander behavior target vector
        Vector3 toTarget = target - transform.position;
        float dist = toTarget.magnitude;
        if (dist <= targetReachThreshold)
        {
            PickNewTarget();
            toTarget = target - transform.position;
        }

        if (toTarget.sqrMagnitude > 0.001f)
            toTarget.Normalize();
        else
            toTarget = transform.forward;

        // If we're below the minimum flight height, bias desired direction upward
        Vector3 desired = toTarget;
        if (transform.position.y < minFlightHeight)
        {
            // add an upward component proportional to how far below the minimum we are
            float deficit = minFlightHeight - transform.position.y;
            desired.y = Mathf.Abs(desired.y) + Mathf.Max(0.5f, deficit);
        }
        else
        {
            // keep a small vertical stability so bats don't dive too low
            desired.y = Mathf.Clamp(desired.y, -0.2f, 1f);
        }

        // Smooth rotation towards desired direction (preserve Y-up)
        Quaternion desiredRot = Quaternion.LookRotation(desired.normalized, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, desiredRot, turnSpeed * Time.fixedDeltaTime);

        // Forward velocity
        rb.linearVelocity = transform.forward * speed;

        // Update animator parameter with current movement speed (if animator present and parameter exists)
        if (animator != null && speedHash != -1)
        {
            float currentSpeed = rb.linearVelocity.magnitude;
            animator.SetFloat(speedHash, currentSpeed);
        }

        // Safety clamp: if physics pushed the bat below min height, correct position slightly
        if (transform.position.y < minFlightHeight - 0.25f)
        {
            Vector3 p = transform.position;
            p.y = Mathf.Lerp(p.y, minFlightHeight, 0.5f);
            rb.position = p;
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, Mathf.Abs(rb.linearVelocity.y) + 1f, rb.linearVelocity.z);
        }
    }

    private void PickNewTarget()
    {
        changeTimer = 0f;

        // Random point inside circle on XZ around home center, random height in range
        Vector2 circle = Random.insideUnitCircle * wanderRadius;
        float y = homeCenter.y + Random.Range(heightRange.x, heightRange.y);

        // enforce minimum flight height
        y = Mathf.Max(y, minFlightHeight);

        target = new Vector3(homeCenter.x + circle.x, y, homeCenter.z + circle.y);
    }

    // Optional: simple collision response so bats don't get stuck
    private void OnCollisionEnter(Collision collision)
    {
        // bump to a new target if hitting something
        PickNewTarget();
    }
}