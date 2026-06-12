
using System.Collections;
using UnityEngine;

/// <summary>
/// Reusable spawner component. Attach to an object that should spawn enemies when clicked or hit.
/// - First hit: attempts a 1-in-N roll to spawn immediately (default 1-in-6). If the roll succeeds the enemies spawn and the object is hidden/destroyed.
/// - If the roll fails on the first hit the object is NOT deleted and subsequent clicks do nothing. (Collisions still apply normal damage after first attempt.)
/// Spawns happen only on compass directions (N, E, S, W) relative to the spawn center.
/// </summary>
public class EnemySpawner : MonoBehaviour
{
    [Header("Health")]
    [Tooltip("Number of hits required to destroy this obstacle (used after the first-hit spawn check).")]
    public int maxHealth = 1;

    [Header("Prefab")]
    public GameObject enemyPrefab;

    [Header("Spawn Settings")]
    [Tooltip("Number of enemies to spawn. They are placed on compass points in order: North, East, South, West.")]
    public int numberOfEnemies = 1;
    [Tooltip("Distance from the spawn center to each compass-point spawn.")]
    public float spawnRadius = 2f;
    [Tooltip("Delay between each spawned enemy (seconds). Set 0 for instant spawn.")]
    public float delayBetweenSpawns = 0f;

    [Header("Trigger")]
    public bool spawnOnObjectClick = true;
    [Tooltip("If true, clicking anywhere on the screen will spawn at the clicked world position (requires groundLayer).")]
    public bool spawnOnScreenClick = false;
    public LayerMask groundLayer;

    [Header("Behavior")]
    public bool destroyAfterSpawn = false;
    [Tooltip("Color to apply when a grave failed to spawn and the required number of subsequent clicks has been reached.")]
    public Color failedClickColor = Color.red;
    [Tooltip("How many clicks after a failed first-attempt are required before applying the failedClickColor.")]
    public int clicksToChangeColor = 2;

    [Header("Limit")]
    [Tooltip("If true, this instance will only allow a single spawn for this object.")]
    public bool limitToOneSpawnPerObject = true;

    [Header("Spawn Chance")]
    [Tooltip("Use a 1-in-N chance for spawning enemies when this object is hit the first time. Set to 6 for 1-in-6.")]
    public int spawnChanceDenominator = 6;

    // Per-instance flag so each object can spawn independently.
    private bool hasSpawned = false;

    // Tracks whether the first-hit spawn attempt has already been performed and whether it failed.
    private bool attemptedFirstHitSpawn = false;
    private bool firstAttemptFailed = false;

    // Count clicks after a failed first attempt
    private int failedClickCount = 0;
    private bool coloredAfterFail = false;

    private bool isSpawning = false;
    private int currentHealth;

    private void Start()
    {
        currentHealth = Mathf.Max(1, maxHealth);
        spawnChanceDenominator = Mathf.Max(1, spawnChanceDenominator);
    }

    private void Update()
    {
        if (spawnOnScreenClick && Input.GetMouseButtonDown(0) && !isSpawning)
        {
            Vector3 spawnCenter;
            if (TryGetMouseWorldPosition(out spawnCenter))
            {
                // screen click uses ground ray so no clicked collider/topY to clamp against
                // screen-click should NOT destroy this spawner object, so pass destroyThis = false
                StartCoroutine(SpawnRoutine(spawnCenter, null, false));
            }
        }
    }

    // Click handling: if first attempt failed, clicking increments a counter and when it reaches clicksToChangeColor the object changes color.
    // Otherwise process the first-hit spawn attempt (or subsequent damage behavior).
    private void OnMouseDown()
    {
        if (!spawnOnObjectClick || isSpawning) return;

        // If the first-hit attempt already failed, count clicks and apply color when threshold is reached.
        if (attemptedFirstHitSpawn && firstAttemptFailed)
        {
            failedClickCount++;
            if (!coloredAfterFail && failedClickCount >= Mathf.Max(1, clicksToChangeColor))
            {
                ApplyFailedClickColor();
                coloredAfterFail = true;
            }
            return;
        }

        Camera cam = Camera.main;
        if (cam == null)
        {
            int res = TryHandleFirstHitSpawn(transform.position, GetComponent<Collider>());
            if (res == -1) // already attempted before (and didn't fail first attempt) => apply damage
                ApplyDamage(1, transform.position, GetComponent<Collider>());
            return;
        }

        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            // Only process clicks that hit this object
            if (hit.collider != null && hit.collider.gameObject == gameObject)
            {
                int res = TryHandleFirstHitSpawn(hit.point, hit.collider);
                if (res == -1)
                {
                    // already attempted previously -> apply normal damage
                    ApplyDamage(1, hit.point, hit.collider);
                }
                // if res == 1 (spawned) or res == 0 (first attempt failed) -> do nothing more on this click
            }
        }
    }

    // Collision handling: on first collision attempt the 1-in-N spawn; afterwards apply standard damage.
    // Note: collisions still apply normal damage after the first-attempt; only clicking is modified after a failed first attempt.
    private void OnCollisionEnter(Collision collision)
    {
        if (!collision.gameObject.CompareTag("Projectile")) return;
        if (isSpawning) return;

        Vector3 hitPoint = collision.contacts.Length > 0 ? collision.contacts[0].point : transform.position;

        int res = TryHandleFirstHitSpawn(hitPoint, GetComponent<Collider>());
        if (res == -1)
        {
            // First attempt already done earlier -> apply normal damage
            ApplyDamage(1, hitPoint, GetComponent<Collider>());
        }
        // if res == 1 (spawned) or res == 0 (first attempt failed) -> do nothing further for this collision
    }

    /// <summary>
    /// Attempts the first-hit spawn.
    /// Returns:
    ///   1 => spawn succeeded and was started,
    ///   0 => this was the first attempt and it failed (object stays; subsequent clicks will count toward color change),
    ///  -1 => first-attempt was already used previously (caller should apply normal damage).
    /// </summary>
    private int TryHandleFirstHitSpawn(Vector3 hitPoint, Collider referenceCollider)
    {
        if (attemptedFirstHitSpawn) return -1; // already tried before (subsequent hits should apply damage)

        attemptedFirstHitSpawn = true;

        // 1-in-N chance to spawn on first hit (1-in-6 by default)
        bool rollSucceeds = Random.Range(0, spawnChanceDenominator) == 0;
        if (!rollSucceeds)
        {
            // First-hit roll failed: mark failure so clicks will be counted going forward.
            firstAttemptFailed = true;
            failedClickCount = 0;
            coloredAfterFail = false;
            return 0;
        }

        // Roll succeeded: compute spawn center/topY and start spawn routine; hide this object
        Collider col = referenceCollider ?? GetComponent<Collider>();
        Vector3 center;
        float topY;
        if (col != null)
        {
            Vector3 boundsCenter = col.bounds.center;
            topY = col.bounds.max.y;
            float y = Mathf.Min(hitPoint.y, boundsCenter.y);
            center = new Vector3(boundsCenter.x, y, boundsCenter.z);
        }
        else
        {
            center = hitPoint;
            topY = transform.position.y;
        }

        // hide visuals/colliders immediately so the object appears gone
        HideGameObject(gameObject);
        // spawn was triggered by this object — destroy it after spawn completes, so pass destroyThis = true
        StartCoroutine(SpawnRoutine(center, topY, true));
        return 1;
    }

    /// <summary>
    /// Apply damage to this obstacle. When health reaches zero it hides the obstacle and starts the spawn routine.
    /// This runs for hits after the first-hit spawn attempt.
    /// </summary>
    public void ApplyDamage(int amount, Vector3? hitPoint = null, Collider referenceCollider = null)
    {
        if (isSpawning) return;
        if (limitToOneSpawnPerObject && hasSpawned) return;

        currentHealth -= amount;
        Debug.Log($"Object Health: {currentHealth}");

        if (currentHealth > 0) return;

        // Compute spawn center and topY using the provided collider if available, else fallback to transform
        Collider col = referenceCollider ?? GetComponent<Collider>();
        Vector3 center;
        float topY;
        if (col != null)
        {
            Vector3 boundsCenter = col.bounds.center;
            topY = col.bounds.max.y;
            float y = hitPoint.HasValue ? Mathf.Min(hitPoint.Value.y, boundsCenter.y) : boundsCenter.y;
            center = new Vector3(boundsCenter.x, y, boundsCenter.z);
        }
        else
        {
            center = hitPoint ?? transform.position;
            topY = transform.position.y;
        }

        // Hide the clicked/hit object immediately (disable renderers & colliders) so it appears gone while spawn happens
        HideGameObject(gameObject);

        // This destruction should apply to the object that took damage and triggered the spawn
        StartCoroutine(SpawnRoutine(center, topY, true));
    }

    private IEnumerator SpawnRoutine(Vector3 center, float? clickedTopY = null, bool destroyThis = false)
    {
        if (enemyPrefab == null)
        {
            Debug.LogWarning("EnemySpawner: No enemyPrefab assigned.");
            yield break;
        }

        // Enforce per-instance limit if requested
        if (limitToOneSpawnPerObject && hasSpawned)
            yield break;

        isSpawning = true;

        for (int i = 0; i < numberOfEnemies; i++)
        {
            Vector3 spawnPos = GetCompassSpawnPosition(center, i, clickedTopY);
            Instantiate(enemyPrefab, spawnPos, Quaternion.identity);

            if (delayBetweenSpawns > 0f)
                yield return new WaitForSeconds(delayBetweenSpawns);
            else
                yield return null; // let one frame pass
        }

        if (limitToOneSpawnPerObject)
            hasSpawned = true;

        isSpawning = false;

        // Destroy this object if requested, or if the global destroyAfterSpawn flag is set.
        if (destroyThis || destroyAfterSpawn)
            Destroy(gameObject);
    }

    /// <summary>
    /// Returns a spawn position placed on a compass direction relative to center.
    /// Order: 0 -> North (+Z), 1 -> East (+X), 2 -> South (-Z), 3 -> West (-X).
    /// If clickedTopY is provided, the spawn Y is clamped so it will not be above that Y.
    /// </summary>
    private Vector3 GetCompassSpawnPosition(Vector3 center, int index, float? clickedTopY = null)
    {
        // Compass directions relative to world axes
        Vector3[] compass = new Vector3[]
        {
            Vector3.forward, // North (+Z)
            Vector3.right,   // East  (+X)
            Vector3.back,    // South (-Z)
            Vector3.left     // West  (-X)
        };

        Vector3 dir = compass[index % compass.Length];
        Vector3 basePos = center + dir * spawnRadius;

        if (clickedTopY.HasValue)
        {
            float spawnY = Mathf.Min(basePos.y, clickedTopY.Value);
            return new Vector3(basePos.x, spawnY, basePos.z);
        }

        return basePos;
    }

    private bool TryGetMouseWorldPosition(out Vector3 worldPos)
    {
        worldPos = Vector3.zero;
        Camera cam = Camera.main;
        if (cam == null) return false;

        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, 100f, groundLayer))
        {
            worldPos = hit.point;
            return true;
        }

        // Fallback: project to Y = transform.position.y plane
        Plane plane = new Plane(Vector3.up, new Vector3(0f, transform.position.y, 0f));
        float enter;
        if (plane.Raycast(ray, out enter))
        {
            worldPos = ray.GetPoint(enter);
            return true;
        }

        return false;
    }

    // Helper: hide visuals and disable colliders so the object "disappears" immediately while this component remains active.
    private void HideGameObject(GameObject target)
    {
        if (target == null) return;

        // Disable all renderers
        Renderer[] rends = target.GetComponentsInChildren<Renderer>(true);
        foreach (var r in rends) r.enabled = false;

        // Disable all colliders
        Collider[] cols = target.GetComponentsInChildren<Collider>(true);
        foreach (var c in cols) c.enabled = false;

        // Optionally disable any lights so they don't remain visible
        Light[] lights = target.GetComponentsInChildren<Light>(true);
        foreach (var l in lights) l.enabled = false;
    }

    // Apply the configured failed-click color to all renderers on this object (called when the post-fail click threshold is reached).
    private void ApplyFailedClickColor()
    {
        Renderer[] rends = GetComponentsInChildren<Renderer>(true);
        foreach (var r in rends)
        {
            if (r == null) continue;
            try
            {
                var mat = r.material;
                if (mat != null && mat.HasProperty("_Color"))
                    mat.color = failedClickColor;
            }
            catch
            {
                // ignore material instancing errors
            }
        }
    }
}