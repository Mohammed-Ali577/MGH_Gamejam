using UnityEngine;

[RequireComponent(typeof(Collider))]
public class Build : MonoBehaviour
{
    [Header("Target Detection")]
    [SerializeField] private string targetTag = "Player";
    [SerializeField] private float detectionRadius = 3f;

    [Header("Movement")]
    [Tooltip("Speed at which the object moves left once the player is within range (units/sec).")]
    [SerializeField] private float moveSpeed = 2f;
    [Tooltip("If > 0 the object will move only this distance then stop. If 0 the object keeps moving while player remains in range.")]
    [SerializeField] private float moveDistance = 0f;
    [Tooltip("If true 'left' is relative to the object's local left (transform.left). Otherwise it is world left (Vector3.left).")]
    [SerializeField] private bool localLeft = true;

    private Transform _target;
    private Rigidbody _rb;
    private bool _isMoving;
    private float _movedSoFar;

    void Start()
    {
        // Try to find the target by tag if not manually assigned
        var found = GameObject.FindWithTag(targetTag);
        if (found != null)
        {
            _target = found.transform;
        }

        _rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        if (_target == null)
        {
            // attempt to locate target every few frames if not found initially
            var found = GameObject.FindWithTag(targetTag);
            if (found != null)
                _target = found.transform;
            else
                return;
        }

        // Check distance to target
        float sqrDist = (_target.position - transform.position).sqrMagnitude;
        if (sqrDist <= detectionRadius * detectionRadius)
        {
            // start moving
            _isMoving = true;
        }
        else
        {
            // stop if target leaves range and moveDistance is zero (continuous only while in range)
            if (moveDistance == 0f)
                _isMoving = false;
        }

        // If moveDistance limit reached, stop
        if (moveDistance > 0f && _movedSoFar >= moveDistance)
        {
            _isMoving = false;
        }
    }

    void FixedUpdate()
    {
        if (!_isMoving)
            return;

        // Transform does not provide `left` property — use negative right for local left
        Vector3 moveDir = localLeft ? -transform.right : Vector3.left;
        Vector3 delta = moveDir.normalized * moveSpeed * Time.fixedDeltaTime;

        if (_rb != null)
        {
            _rb.MovePosition(_rb.position + delta);
        }
        else
        {
            transform.position += delta;
        }

        if (moveDistance > 0f)
        {
            _movedSoFar += delta.magnitude;
            if (_movedSoFar >= moveDistance)
                _isMoving = false;
        }
    }

    // Optional: draw detection radius in editor
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
        Gizmos.color = Color.cyan;
        Vector3 dir = localLeft ? -transform.right : Vector3.left;
        Gizmos.DrawLine(transform.position, transform.position + dir.normalized * 1f);
    }
}
