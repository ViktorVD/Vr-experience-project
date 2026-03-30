using UnityEngine;

public sealed class Sword : MonoBehaviour
{
    [SerializeField] private float damageBase = 20f;
    [SerializeField] private float minVelocityForDamage = 2f;
    [SerializeField] private Rigidbody rb;

    public string targetTag;

    private void OnTriggerEnter(Collider other)
    {

        if (other.CompareTag(targetTag))
        {
            float speed = rb.linearVelocity.magnitude;

            if (speed >= minVelocityForDamage)
            {
                Health targetHealth = other.GetComponentInParent<Health>();
                if (targetHealth != null)
                {
                    targetHealth.TakeDamage(damageBase);
                    Debug.Log($"{gameObject.name} raakte {other.name} met snelheid {speed}!");
                }
            }
        }


        if (other.CompareTag("Sword"))
        {
            Debug.Log("Zwaarden raaken elkaar");
        }
    }
}