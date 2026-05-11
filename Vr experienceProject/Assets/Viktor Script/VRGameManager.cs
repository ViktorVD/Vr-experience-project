using UnityEngine;
using UnityEngine.Events;

public class VRGameManager : MonoBehaviour
{
    [Header("Spelers & Health")]
    [Tooltip("Health script van de VR Speler")]
    public Health playerHealth;
    [Tooltip("Health script van de AI Bot")]
    public Health aiHealth;
    [Tooltip("Het AI script van de bot")]
    public VRCombatAgent aiAgent;
    [Tooltip("De complete transform (bijv. de XR Rig) van de speler om te verplaatsen")]
    public Transform playerRig;

    [Header("Spawn Configuraties")]
    public Transform aiSpawnPoint;
    public Transform playerSpawnPoint;

    [Header("Spel Regels")]
    public int totaalRondes = 3;
    private int huidigeRonde = 1;
    private int spelerWins = 0;
    private int aiWins = 0;
    private bool isBezig = false;

    [Header("Events (Koppel aan UI of geluiden)")]
    public UnityEvent OnSpelGestart;
    public UnityEvent OnRondeGewonnen;
    public UnityEvent OnRondeVerloren;
    public UnityEvent OnSpelGewonnen;
    public UnityEvent OnSpelVerloren;

    void Start()
    {
        // Koppel de health events vast aan de functies
        if (playerHealth != null)
            playerHealth.OnDeath.AddListener(SpelerVerliest);
            
        if (aiHealth != null)
            aiHealth.OnDeath.AddListener(SpelerWint);

        // Zorg dat de AI uit staat in het begin tot het spel gestart is
        if (aiAgent != null)
            aiAgent.gameObject.SetActive(false);
    }

    // Deze functie kan je aanroepen via een UI knop in VR (bijv. met je laser pointer) of een controller knop
    public void StartVolgendeRonde()
    {
        if (huidigeRonde > totaalRondes)
        {
            Debug.Log("Alle rondes zijn al gespeeld! Roep ResetSpel() aan om opnieuw te beginnen.");
            return;
        }

        Debug.Log($"Start Ronde {huidigeRonde}");
        isBezig = true;

        // 1. Speler respawnen
        if (playerRig != null && playerSpawnPoint != null)
        {
            playerRig.position = playerSpawnPoint.position;
            playerRig.rotation = playerSpawnPoint.rotation;
        }
        if (playerHealth != null) playerHealth.ResetHealth();

        // 2. AI respawnen
        if (aiAgent != null && aiSpawnPoint != null)
        {
            aiAgent.transform.position = aiSpawnPoint.position;
            aiAgent.transform.rotation = aiSpawnPoint.rotation;
            
            // Activeer de AI en start een nieuwe episode voor ML-Agents
            aiAgent.gameObject.SetActive(true);
            aiAgent.EndEpisode(); 
        }
        if (aiHealth != null) aiHealth.ResetHealth();

        OnSpelGestart?.Invoke();
    }

    private void SpelerWint()
    {
        if (!isBezig) return;
        isBezig = false;

        Debug.Log("AI is dood! Speler wint de ronde.");
        spelerWins++;
        StopAI();

        OnRondeGewonnen?.Invoke();
        CheckEindeSpel();
    }

    private void SpelerVerliest()
    {
        if (!isBezig) return;
        isBezig = false;

        Debug.Log("Speler is dood! AI wint de ronde.");
        aiWins++;
        StopAI();

        OnRondeVerloren?.Invoke();
        CheckEindeSpel();
    }

    private void StopAI()
    {
        if (aiAgent != null)
        {
            // Zet AI uit zodat hij stopt met bewegen of aanvallen
            aiAgent.gameObject.SetActive(false);
        }
    }

    private void CheckEindeSpel()
    {
        huidigeRonde++;

        // Controleer of de 3 rondes voorbij zijn
        if (huidigeRonde > totaalRondes)
        {
            if (spelerWins > aiWins)
            {
                Debug.Log($"Speler wint het spel met {spelerWins}-{aiWins}!");
                OnSpelGewonnen?.Invoke();
            }
            else
            {
                Debug.Log($"AI wint het spel met {aiWins}-{spelerWins}!");
                OnSpelVerloren?.Invoke();
            }
        }
    }

    // Optioneel: Om na een compleet spel (3 rondes) weer vanaf ronde 1 te laten beginnen
    public void ResetSpel()
    {
        huidigeRonde = 1;
        spelerWins = 0;
        aiWins = 0;
        StartVolgendeRonde();
    }
}
