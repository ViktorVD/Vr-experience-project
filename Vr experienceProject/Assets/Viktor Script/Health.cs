using UnityEngine;
using UnityEngine.Events;

public sealed class Health : MonoBehaviour
{
    [SerializeField] private float maxHealth = 100f;
    public float currentHealth;

    public UnityEvent OnDeath;
    public UnityEvent OnTakeDamage;

    void Start() => ResetHealth();

    public void ResetHealth()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(float amount)
    {
        currentHealth -= amount;
        OnTakeDamage?.Invoke();

        if (currentHealth <= 0)
        {
            currentHealth = 0;
            OnDeath?.Invoke();
        }
    }
}