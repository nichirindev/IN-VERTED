using UnityEngine;

public class Sword : MonoBehaviour
{
    [Header("References")]
    public EnemyAttack parentEnemyAttack;

    [Header("Settings")]
    public float damage = 25f;
    public float knockbackForce = 8f;
    public bool hitPlayer;

    private bool wasSwinging;

    private void Reset()
    {
        if (parentEnemyAttack == null)
            parentEnemyAttack = GetComponentInParent<EnemyAttack>();
    }

    private void Awake()
    {
        if (parentEnemyAttack == null)
            parentEnemyAttack = GetComponentInParent<EnemyAttack>();
    }

    private void Update()
    {
        if (parentEnemyAttack == null) return;

        if (parentEnemyAttack.isSwinging && !wasSwinging)
        {
            hitPlayer = false;
        }
        wasSwinging = parentEnemyAttack.isSwinging;
    }

    private void OnTriggerStay(Collider other)
    {
        if (parentEnemyAttack == null || !parentEnemyAttack.isSwinging)
            return;

        if (hitPlayer)
            return;

        PlayerHealth health = other.GetComponent<PlayerHealth>();
        if (health == null)
            health = other.GetComponentInParent<PlayerHealth>();

        if (health != null)
        {
            health.TakeDamage(damage);

            Rigidbody rb = other.GetComponent<Rigidbody>();
            if (rb == null)
                rb = other.GetComponentInParent<Rigidbody>();

            if (rb != null)
            {
                Vector3 dir = (other.transform.position - transform.position).normalized;
                dir.y = 0.3f;
                rb.AddForce(dir * knockbackForce, ForceMode.Impulse);
            }

            hitPlayer = true;
        }
    }
}
