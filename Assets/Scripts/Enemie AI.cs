
using UnityEngine;

public class EnemyAI : MonoBehaviour

{

    [Header("Target")]

    [SerializeField] private Transform target;

    [SerializeField] private string targetTag = "Player";

    [Header("Movement")]

    [SerializeField] private float moveSpeed = 3f;

    [SerializeField] private float rotationSpeed = 10f;

    [SerializeField] private float stoppingDistance = 1f;

    [SerializeField] private float chaseRange = 20f;

    private Rigidbody _rb;

    private Vector3 _rbVelocity;

    void Start()

    {

        if (target == null)

        {

            var playerObj = GameObject.FindWithTag(targetTag);

            if (playerObj != null)

            {

                target = playerObj.transform;

            }

        }

        _rb = GetComponent<Rigidbody>();

    }

    void Update()

    {

        if (target == null)

        {

            return;

        }

        Vector3 toTarget = target.position - transform.position;

        float distance = toTarget.magnitude;

        // Only chase when inside chaseRange

        if (distance > chaseRange)

        {

            _rbVelocity = Vector3.zero;

            return;

        }

        Vector3 direction = toTarget.normalized;

        // Smoothly rotate to face the target

        if (direction.sqrMagnitude > 0f)

        {

            Quaternion targetRot = Quaternion.LookRotation(direction);

            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);

        }

        // Move toward the target until within stoppingDistance

        if (distance > stoppingDistance)

        {

            if (_rb != null)

            {

                // Defer physics movement to FixedUpdate

                _rbVelocity = direction * moveSpeed;

            }

            else

            {

                transform.position += direction * moveSpeed * Time.deltaTime;

            }

        }

        else

        {

            _rbVelocity = Vector3.zero;

        }

    }

    void FixedUpdate()

    {

        if (_rb == null)

        {

            return;

        }

        if (_rbVelocity.sqrMagnitude > 0f)

        {

            _rb.MovePosition(_rb.position + _rbVelocity * Time.fixedDeltaTime);

        }

    }

    void OnDrawGizmosSelected()

    {

        Gizmos.color = Color.red;

        Gizmos.DrawWireSphere(transform.position, chaseRange);

        Gizmos.color = Color.yellow;

        Gizmos.DrawWireSphere(transform.position, stoppingDistance);

    }

    // Collision handlers to end the game when enemy touches the player.
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.collider != null && collision.collider.CompareTag(targetTag))
        {
            if (GameManager.Instance != null && !GameManager.Instance.GameOver)
                GameManager.Instance.EndGame();
            else
                Time.timeScale = 0f; // fallback if GameManager not present
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other != null && other.CompareTag(targetTag))
        {
            if (GameManager.Instance != null && !GameManager.Instance.GameOver)
                GameManager.Instance.EndGame();
            else
                Time.timeScale = 0f; // fallback if GameManager not present
        }
    }

}