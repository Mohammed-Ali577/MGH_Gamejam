using UnityEngine;
using System.Collections;

public class BatSpawner : MonoBehaviour
{
    [Header("Prefab")]
    public GameObject batPrefab;

    [Header("Spawn")]
    public int batCount = 6;
    [Tooltip("Radius around this spawner where bats will be spawned and roam.")]
    public float spawnRadius = 2f;
    [Tooltip("Height at which bats are spawned relative to spawner position.")]
    public float spawnHeight = 2f;
    [Tooltip("Optional stagger between spawn instances (0 = all at once).")]
    public float staggerDelay = 0.1f;

    [Header("Behavior")]
    public bool spawnOnStart = true;
    [Tooltip("If true, calling SpawnBats repeatedly will instantiate additional bats. Otherwise only first spawn is allowed.")]
    public bool allowMultipleSpawns = false;

    private bool hasSpawned = false;

    private void Start()
    {
        if (spawnOnStart)
            SpawnBats();
    }

    public void SpawnBats()
    {
        if (!allowMultipleSpawns && hasSpawned) return;
        if (batPrefab == null)
        {
            Debug.LogWarning("BatSpawner: batPrefab is not assigned.");
            return;
        }

        StartCoroutine(SpawnRoutine());
        hasSpawned = true;
    }

    private IEnumerator SpawnRoutine()
    {
        Vector3 center = transform.position;

        for (int i = 0; i < batCount; i++)
        {
            Vector2 circle = Random.insideUnitCircle * spawnRadius;
            Vector3 pos = new Vector3(center.x + circle.x, center.y + spawnHeight, center.z + circle.y);

            GameObject go = Instantiate(batPrefab, pos, Quaternion.identity);
            Bat batComp = go.GetComponent<Bat>();
            if (batComp != null)
            {
                // give each bat the spawner center so they roam around this spawner
                batComp.Initialize(center);
            }

            if (staggerDelay > 0f)
                yield return new WaitForSeconds(staggerDelay);
            else
                yield return null;
        }
    }

#if UNITY_EDITOR
    // Draw spawn radius in editor
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position + Vector3.up * spawnHeight, spawnRadius);
    }
#endif
}
