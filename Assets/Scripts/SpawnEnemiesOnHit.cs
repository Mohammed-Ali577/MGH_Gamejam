using UnityEngine;

public class SpawnEnemiesOnHit : MonoBehaviour

{

    [Header("Health")]

    public int maxHealth = 3;

    [Header("Enemy Settings")]

    public GameObject enemyPrefab;

    public int numberOfEnemies = 5;

    public float spawnRadius = 5f;

    private int currentHealth;

    private bool hasSpawned = false;

    private void Start()

    {

        currentHealth = maxHealth;

    }

    private void OnCollisionEnter(Collision collision)

    {

        // Only take damage from objects tagged "Projectile"

        if (collision.gameObject.CompareTag("Projectile"))

        {

            TakeDamage(1);

        }

    }

    public void TakeDamage(int damage)

    {

        if (hasSpawned) return;

        currentHealth -= damage;

        Debug.Log($"Object Health: {currentHealth}");

        if (currentHealth <= 0)

        {

            hasSpawned = true;

            SpawnEnemies();

            // Optional: destroy this object after spawning enemies

            Destroy(gameObject);

        }

    }

    private void SpawnEnemies()

    {

        for (int i = 0; i < numberOfEnemies; i++)

        {

            Vector3 spawnPosition = GetRandomSpawnPosition();

            Instantiate(

                enemyPrefab,

                spawnPosition,

                Quaternion.identity

            );

        }

    }

    private Vector3 GetRandomSpawnPosition()

    {

        Vector2 randomCircle = Random.insideUnitCircle * spawnRadius;

        return transform.position +

               new Vector3(randomCircle.x, 0f, randomCircle.y);

    }

}
