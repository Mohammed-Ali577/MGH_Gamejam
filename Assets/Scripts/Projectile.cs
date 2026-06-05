using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Projectile : MonoBehaviour
{
    [Header("Projectile")]
    [SerializeField] private float _lifeTime = 5f;
    [SerializeField] private float _impactForce = 10f;

    private Rigidbody _rb;

    void Start()
    {
        _rb = GetComponent<Rigidbody>();
        Destroy(gameObject, _lifeTime);
    }

    void OnCollisionEnter(Collision collision)
    {
        // Apply impulse to hit rigidbody (if any) then destroy projectile
        if (collision.rigidbody != null)
        {
            Vector3 impulseDir = _rb != null && _rb.linearVelocity.sqrMagnitude > 0.001f ? _rb.linearVelocity.normalized : transform.forward;
            collision.rigidbody.AddForceAtPosition(impulseDir * _impactForce, collision.contacts[0].point, ForceMode.Impulse);
        }

        Destroy(gameObject);
    }
}

public class ProjectileSpawner : MonoBehaviour
{
    [Header("Spawner")]
    [SerializeField] private GameObject _projectilePrefab;
    [SerializeField] private Transform _firingPoint; // if null, will search for GameObject named "firing point"
    [SerializeField] private float _projectileSpeed = 30f;
    [SerializeField] private float _fireRate = 0.2f;

    private float _lastFireTime;

    void Start()
    {
        if (_firingPoint == null)
        {
            var fpGo = GameObject.Find("firing point");
            if (fpGo != null)
            {
                _firingPoint = fpGo.transform;
            }
            else
            {
                Debug.LogWarning("ProjectileSpawner: no firing point assigned and GameObject named \"firing point\" not found.");
            }
        }
    }

    void Update()
    {
        // Left mouse click
        if (Input.GetMouseButtonDown(0) && Time.time - _lastFireTime >= _fireRate)
        {
            Fire();
            _lastFireTime = Time.time;
        }
    }

    private void Fire()
    {
        if (_projectilePrefab == null)
        {
            Debug.LogWarning("ProjectileSpawner: projectile prefab not assigned.");
            return;
        }

        if (_firingPoint == null)
        {
            Debug.LogWarning("ProjectileSpawner: firing point not assigned or found.");
            return;
        }

        var proj = Instantiate(_projectilePrefab, _firingPoint.position, _firingPoint.rotation);
        var rb = proj.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = _firingPoint.forward * _projectileSpeed;
        }
    }
}
