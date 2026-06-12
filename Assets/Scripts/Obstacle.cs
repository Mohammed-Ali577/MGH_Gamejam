using System.Collections;
using UnityEngine;

public class Obstacle : MonoBehaviour
{
    [Header("Fall Settings")]
    [Tooltip("Delay in seconds after player contact before the obstacle starts falling.")]
    [SerializeField] private float fallDelay = 0f;
    [Tooltip("Optional impulse applied downward when the obstacle starts to fall.")]
    [SerializeField] private float fallForce = 2f;
    [Tooltip("If true the script listens for trigger events; otherwise it listens for physics collisions.")]
    [SerializeField] private bool useTrigger = false;

    [Header("Reset / Cleanup")]
    [Tooltip("If > 0 the obstacle will reset back to its original position after this delay (seconds). Set to 0 to disable reset.)")]
    [SerializeField] private float resetDelay = 0f;
    [Tooltip("If true the obstacle GameObject will be destroyed after falling (uses destroyDelay).")]
    [SerializeField] private bool destroyAfterFall = false;
    [SerializeField] private float destroyDelay = 5f;

    private Rigidbody _rb;
    private Vector3 _startPosition;
    private Quaternion _startRotation;
    private bool _hasFallen;

    void Start()
    {
        // Record start transform for optional reset
        _startPosition = transform.position;
        _startRotation = transform.rotation;

        // Ensure a Rigidbody exists so gravity can be enabled when falling.
        _rb = GetComponent<Rigidbody>();
        if (_rb == null)
        {
            // Add a Rigidbody and keep it kinematic until triggered so the obstacle stays in place
            _rb = gameObject.AddComponent<Rigidbody>();
            _rb.isKinematic = true;
            _rb.useGravity = false;
        }
        else
        {
            // If a Rigidbody already exists, start kinematic so the obstacle stays locked in place.
            _rb.isKinematic = true;
            _rb.useGravity = false;
        }
    }

    // Collision-based trigger
    void OnCollisionEnter(Collision collision)
    {
        if (useTrigger || _hasFallen)
            return;

        if (collision.gameObject.CompareTag("Player"))
        {
            StartCoroutine(FallRoutine());
        }
    }

    // Trigger-based trigger (for trigger colliders)
    void OnTriggerEnter(Collider other)
    {
        if (!useTrigger || _hasFallen)
            return;

        if (other.gameObject.CompareTag("Player"))
        {
            StartCoroutine(FallRoutine());
        }
    }

    private IEnumerator FallRoutine()
    {
        // Optional delay before falling
        if (fallDelay > 0f)
            yield return new WaitForSeconds(fallDelay);

        if (_hasFallen)
            yield break;

        _hasFallen = true;

        // Enable physics so the obstacle falls
        _rb.isKinematic = false;
        _rb.useGravity = true;

        if (fallForce != 0f)
        {
            _rb.AddForce(Vector3.down * Mathf.Abs(fallForce), ForceMode.Impulse);
        }

        if (destroyAfterFall)
        {
            if (destroyDelay > 0f)
                Destroy(gameObject, destroyDelay);
            else
                Destroy(gameObject);
        }
        else if (resetDelay > 0f)
        {
            // Wait resetDelay seconds, then reset to starting pose
            yield return new WaitForSeconds(resetDelay);
            ResetObstacle();
        }
    }

    private void ResetObstacle()
    {
        // Stop physics and restore starting transform
        _rb.isKinematic = true;
        _rb.useGravity = false;
        _rb.linearVelocity = Vector3.zero;
        _rb.angularVelocity = Vector3.zero;

        transform.position = _startPosition;
        transform.rotation = _startRotation;

        _hasFallen = false;
    }

    // Optional editor helper to visualize starting position
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, 0.1f);
    }
}
