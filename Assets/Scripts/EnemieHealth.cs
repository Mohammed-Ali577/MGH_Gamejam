
using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    public float health = 100f;
    public float clickDamage = 25f;

    [Tooltip("1-in-N chance the click will NOT deal damage. Set to 4 for a 1-in-4 chance.")]
    public int missChanceDenominator = 4;

    private void OnMouseDown()
    {
        // sanitize
        if (missChanceDenominator < 1) missChanceDenominator = 1;

        // 1-in-N chance to ignore this click (no damage)
        if (Random.Range(0, missChanceDenominator) == 0)
        {
            Debug.Log("Click missed — no damage applied.");
            return;
        }

        health -= clickDamage;
        Debug.Log($"Health: {health}");

        if (health <= 0f)
        {
            Destroy(gameObject);
        }
    }
}