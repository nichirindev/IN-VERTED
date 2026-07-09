using UnityEngine;

[RequireComponent(typeof(EnemyAttack))]
public class EnemyAI : MonoBehaviour
{
    [Header("Detection")]
    public float detectionRange = 10f;
    public float attackRange = 2.5f;
    public LayerMask playerLayer;

    [Header("Timing")]
    public float attackCooldown = 1.5f;

    private EnemyAttack enemyAttack;
    private HumanoidBalance balance;
    private float cooldownTimer;
    private Transform target;

    private void Start()
    {
        enemyAttack = GetComponent<EnemyAttack>();
        balance = GetComponent<HumanoidBalance>();
    }

    private void FixedUpdate()
    {
        if (balance != null && balance.state == HumanoidBalance.BalanceState.dead)
            return;

        if (balance != null && (balance.state == HumanoidBalance.BalanceState.falling
            || balance.state == HumanoidBalance.BalanceState.tumbling))
            return;

        FindTarget();

        if (target != null)
        {
            Vector3 dir = (target.position - transform.position);
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.001f)
            {
                dir = dir.normalized;
                if (balance != null)
                    balance.RotateBody(dir);
            }
        }

        if (cooldownTimer > 0f)
            cooldownTimer -= Time.fixedDeltaTime;

        if (enemyAttack.isSwinging)
            return;

        if (cooldownTimer > 0f)
            return;

        if (target != null)
        {
            float dist = Vector3.Distance(transform.position, target.position);
            if (dist <= attackRange)
            {
                enemyAttack.Swing();
                cooldownTimer = attackCooldown;
            }
        }
    }

    private void FindTarget()
    {
        target = null;
        Collider[] hits = Physics.OverlapSphere(transform.position, detectionRange, playerLayer);
        float closest = float.MaxValue;

        foreach (Collider hit in hits)
        {
            PlayerHealth health = hit.GetComponent<PlayerHealth>();
            if (health == null) health = hit.GetComponentInParent<PlayerHealth>();
            if (health == null) continue;

            float dist = Vector3.Distance(transform.position, hit.transform.position);
            if (dist < closest)
            {
                closest = dist;
                target = hit.transform;
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
