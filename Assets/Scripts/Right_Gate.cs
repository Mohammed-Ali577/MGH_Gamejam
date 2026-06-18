using UnityEngine;

[RequireComponent(typeof(Collider))]
public class Right_Gate : MonoBehaviour
{
    [Header("Target Detection")]
    [SerializeField] private string targetTag = "Player";
    [SerializeField] private float detectionRadius = 3f;

    [Header("Movement")]
    [Tooltip("Speed at which the object moves right once the player is within range (units/sec).")]
    [SerializeField] private float moveSpeed = 2f;
    [Tooltip("If > 0 the object will move only this distance then stop. If 0 the object keeps moving while player remains in range.")]
    [SerializeField] private float moveDistance = 0f;
    [Tooltip("If true 'right' is relative to the object's local right (transform.right). Otherwise it is world right (Vector3.right).")]
    [SerializeField] private bool localRight = true;

    private Transform _target;
    private Rigidbody _rb;
    private bool _isMoving;
    private float _movedSoFar;

    void Start()
    {
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
            var found = GameObject.FindWithTag(targetTag);
            if (found != null)
                _target = found.transform;
            else
                return;
        }

        float sqrDist = (_target.position - transform.position).sqrMagnitude;
        if (sqrDist <= detectionRadius * detectionRadius)
        {
            _isMoving = true;
        }
        else
        {
            if (moveDistance == 0f)
                _isMoving = false;
        }

        if (moveDistance > 0f && _movedSoFar >= moveDistance)
        {
            _isMoving = false;
        }
    }

    void FixedUpdate()
    {
        if (!_isMoving)
            return;

        Vector3 moveDir = localRight ? transform.right : Vector3.right;
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

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
        Gizmos.color = Color.cyan;
        Vector3 dir = localRight ? transform.right : Vector3.right;
        Gizmos.DrawLine(transform.position, transform.position + dir.normalized * 1f);
    }
}
