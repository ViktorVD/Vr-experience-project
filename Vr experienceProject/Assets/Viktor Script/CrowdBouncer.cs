using UnityEngine;

public class CrowdBouncer : MonoBehaviour
{
    [Header("Bounce Instellingen")]
    [Tooltip("Hoe hoog moet het karakter springen?")]
    public float bounceHeight = 0.4f;
    [Tooltip("Hoe lang duurt 1 sprong in seconden?")]
    public float bounceDuration = 0.5f;
    
    [Header("Wachttijd Instellingen")]
    [Tooltip("Minimum aantal seconden tussen elke sprong")]
    public float minWaitTime = 0.5f;
    [Tooltip("Maximum aantal seconden tussen elke sprong (Zorgt voor de random willekeur)")]
    public float maxWaitTime = 3f;

    private float waitTimer;
    private float bounceTimer;
    private bool isBouncing = false;
    private float originalY;

    void Start()
    {
        // Bewaar de start-hoogte zodat we altijd mooi terug op de grond landen
        originalY = transform.localPosition.y;
        
        // Kies een willekeurige eerste wachttijd zodat niet iedereen tegelijk springt!
        waitTimer = Random.Range(0f, maxWaitTime);
    }

    void Update()
    {
        if (isBouncing)
        {
            bounceTimer += Time.deltaTime;
            float t = bounceTimer / bounceDuration;
            
            // Een sinus-golf maakt een hele vloeiende sprong-beweging (omhoog en terug omlaag)
            float currentHeight = originalY + Mathf.Sin(t * Mathf.PI) * bounceHeight;
            
            // Pas de hoogte aan
            Vector3 pos = transform.localPosition;
            pos.y = currentHeight;
            transform.localPosition = pos;

            // Stop de sprong zodra de tijd om is
            if (t >= 1f)
            {
                pos.y = originalY; // Zorg dat hij EXACT op de grond staat
                transform.localPosition = pos;
                
                isBouncing = false;
                
                // Kies een nieuwe willekeurige wachttijd voor de volgende sprong
                waitTimer = Random.Range(minWaitTime, maxWaitTime);
            }
        }
        else
        {
            waitTimer -= Time.deltaTime;
            if (waitTimer <= 0f)
            {
                isBouncing = true;
                bounceTimer = 0f;
            }
        }
    }
}
