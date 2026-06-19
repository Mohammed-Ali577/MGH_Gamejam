using UnityEngine;

[RequireComponent(typeof(Collider))]
public class NewMonoBehaviourScript : MonoBehaviour
{
    [Header("Remove on contact")]
    [Tooltip("Delay in seconds before the object is removed after player contact.")]
    [SerializeField] private float destroyDelay = 0f;

    private bool _scheduled;

    // Collision-based contact (non-trigger collider)
    void OnCollisionEnter(Collision collision)
    {
        if (_scheduled)
            return;

        if (collision.gameObject.CompareTag("Player"))
        {
            _scheduled = true;
            Destroy(gameObject, Mathf.Max(0f, destroyDelay));
        }
    }

    // Trigger-based contact (trigger collider)
    void OnTriggerEnter(Collider other)
    {
        if (_scheduled)
            return;

        if (other.gameObject.CompareTag("Player"))
        {
            _scheduled = true;
            Destroy(gameObject, Mathf.Max(0f, destroyDelay));
        }
    }
}
