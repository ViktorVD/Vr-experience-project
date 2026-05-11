using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public sealed class Health : MonoBehaviour
{
    [SerializeField] private float maxHealth = 50f;
    public float currentHealth;
    [Tooltip("Sleep hier de UI Slider in voor de Healthbar (optioneel)")]
    public Slider healthBar;

    [Header("Visual Effects")]
    [Tooltip("Sleep hier de 3D modellen (Renderers) in die rood moeten knipperen bij damage")]
    public Renderer[] modelRenderers;
    private Color[] originalColors;

    public UnityEvent OnDeath;
    public UnityEvent OnTakeDamage;

    void Start()
    {
        ResetHealth();
        
        // Bewaar de originele kleuren van het model
        if (modelRenderers != null && modelRenderers.Length > 0)
        {
            originalColors = new Color[modelRenderers.Length];
            for (int i = 0; i < modelRenderers.Length; i++)
            {
                if (modelRenderers[i] != null && modelRenderers[i].material != null)
                {
                    // Check of het URP of Standard is
                    if (modelRenderers[i].material.HasProperty("_BaseColor"))
                        originalColors[i] = modelRenderers[i].material.GetColor("_BaseColor");
                    else
                        originalColors[i] = modelRenderers[i].material.color;
                }
            }
        }
    }

    public void ResetHealth()
    {
        currentHealth = maxHealth;
        if (healthBar != null)
        {
            healthBar.maxValue = maxHealth;
            healthBar.value = currentHealth;
        }
    }

    public void TakeDamage(float amount)
    {
        currentHealth -= amount;
        if (healthBar != null) healthBar.value = currentHealth;
        
        if (modelRenderers != null && modelRenderers.Length > 0 && gameObject.activeInHierarchy)
        {
            StartCoroutine(FlashRedCoroutine());
        }
        
        OnTakeDamage?.Invoke();

        if (currentHealth <= 0)
        {
            currentHealth = 0;
            OnDeath?.Invoke();
        }
    }

    private System.Collections.IEnumerator FlashRedCoroutine()
    {
        // Zet alles rood
        for (int i = 0; i < modelRenderers.Length; i++)
        {
            if (modelRenderers[i] != null)
            {
                if (modelRenderers[i].material.HasProperty("_BaseColor"))
                    modelRenderers[i].material.SetColor("_BaseColor", Color.red);
                else
                    modelRenderers[i].material.color = Color.red;
            }
        }
        
        yield return new WaitForSeconds(0.15f);

        // Zet alles terug naar normaal
        for (int i = 0; i < modelRenderers.Length; i++)
        {
            if (modelRenderers[i] != null)
            {
                if (modelRenderers[i].material.HasProperty("_BaseColor"))
                    modelRenderers[i].material.SetColor("_BaseColor", originalColors[i]);
                else
                    modelRenderers[i].material.color = originalColors[i];
            }
        }
    }
}