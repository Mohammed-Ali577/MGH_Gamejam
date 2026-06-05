using UnityEngine;

public class PressureGun : MonoBehaviour
{
    [Header("Projectile")]
    [SerializeField] private GameObject _projectilePrefab;
    [SerializeField] private float _projectileSpeed = 30f;
    [SerializeField] private float _projectileLifeTime = 5f;

    [Header("Firing")]
    [SerializeField] private Transform _muzzle;
    [SerializeField] private float _fireRate = 0.2f; // seconds between shots
    [SerializeField] private float _impactForce = 10f;

    private float _lastFireTime;

    void Start()
    {
        if (_muzzle == null)
        {
            // default to this transform if no muzzle assigned
            _muzzle = transform;
        }
    }

    void Update()
    {
        // Left mouse button pressed
        if (Input.GetMouseButtonDown(0) && Time.time - _lastFireTime >= _fireRate)
        {
            Fire();
            _lastFireTime = Time.time;
        }
    }

    private void Fire()
    {
        // If a projectile prefab is assigned, spawn and propel it
        if (_projectilePrefab != null)
        {
            var proj = Instantiate(_projectilePrefab, _muzzle.position, _muzzle.rotation);
            var rb = proj.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = _muzzle.forward * _projectileSpeed;
            }
            else
            {
                // if prefab has no Rigidbody, try to move it forward manually
                proj.transform.position += _muzzle.forward * 0.01f;
            }

            Destroy(proj, _projectileLifeTime);
            return;
        }

        // Fallback: raycast and apply impulse to hit rigidbodies
        Ray ray = new Ray(_muzzle.position, _muzzle.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, ~0, QueryTriggerInteraction.Ignore))
        {
            var hitRb = hit.rigidbody;
            if (hitRb != null)
            {
                hitRb.AddForceAtPosition(_muzzle.forward * _impactForce, hit.point, ForceMode.Impulse);
            }
        }
    }
}
