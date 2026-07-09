using UnityEngine;
using System.Collections;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health")]
    public float maxHealth = 100f;
    public float currentHealth;

    [Header("Respawn")]
    public Transform respawnPoint;
    public float respawnDelay = 2f;

    public System.Action<float, float> OnHealthChanged;
    public System.Action OnPlayerDied;

    private Vector3 startPosition;

    private void Awake()
    {
        currentHealth = maxHealth;
        startPosition = transform.position;
    }

    public void TakeDamage(float amount)
    {
        if (PlayerMovement.Instance != null && PlayerMovement.Instance.dead)
            return;

        currentHealth -= amount;
        currentHealth = Mathf.Max(currentHealth, 0f);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        if (currentHealth <= 0f)
            Die();
    }

    public void Heal(float amount)
    {
        currentHealth += amount;
        currentHealth = Mathf.Min(currentHealth, maxHealth);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    private void Die()
    {
        if (PlayerMovement.Instance != null)
            PlayerMovement.Instance.dead = true;

        if (PlayerInput.Instance != null)
            PlayerInput.Instance.active = false;

        OnPlayerDied?.Invoke();
        StartCoroutine(RespawnCoroutine());
    }

    private IEnumerator RespawnCoroutine()
    {
        yield return new WaitForSeconds(respawnDelay);

        Vector3 spawnPos = respawnPoint != null ? respawnPoint.position : startPosition;
        transform.position = spawnPos;

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        currentHealth = maxHealth;
        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        if (PlayerMovement.Instance != null)
            PlayerMovement.Instance.dead = false;

        if (PlayerInput.Instance != null)
            PlayerInput.Instance.active = true;
    }
}
