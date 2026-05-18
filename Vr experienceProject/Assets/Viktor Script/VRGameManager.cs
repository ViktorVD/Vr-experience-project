using UnityEngine;
using UnityEngine.Events;
using TMPro;

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

    [Header("UI Elementen")]
    [Tooltip("Het Canvas dat verborgen moet worden tijdens het gevecht")]
    public GameObject startMenuCanvas;
    [Tooltip("De Tekst op het canvas die zegt 'Ronde 1', 'Je Wint!', etc.")]
    public TextMeshProUGUI statusTekst;

    [Header("Spawn Configuraties")]
    public Transform aiSpawnPoint;
    public Transform playerSpawnPoint;

    [Header("Spel Regels")]
    public int totaalRondes = 3;
    private int huidigeRonde = 1;
    private int spelerWins = 0;
    private int aiWins = 0;
    private bool isBezig = false;

    [Header("Audio")]
    public AudioSource gameAudioSource;
    [Tooltip("Apart AudioSource voor achtergrondmuziek (Zet Play On Awake uit!)")]
    public AudioSource bgmAudioSource;
    public AudioClip battleMusic;
    public AudioClip startRondeSound;
    public AudioClip winRondeSound;
    public AudioClip loseRondeSound;
    public AudioClip winGameSound;
    public AudioClip loseGameSound;

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
            // Als het spel al gedaan is en de speler klikt opnieuw op start, begin dan helemaal opnieuw!
            ResetSpel();
            return;
        }

        Debug.Log($"Start Ronde {huidigeRonde}");
        isBezig = true;

        // 0. Verberg het menu
        if (startMenuCanvas != null)
        {
            startMenuCanvas.SetActive(false);
        }

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

        if (gameAudioSource != null && startRondeSound != null) gameAudioSource.PlayOneShot(startRondeSound);
        
        // Start de battle music
        if (bgmAudioSource != null && battleMusic != null && !bgmAudioSource.isPlaying)
        {
            bgmAudioSource.clip = battleMusic;
            bgmAudioSource.loop = true;
            bgmAudioSource.Play();
        }

        OnSpelGestart?.Invoke();
    }

    private void SpelerWint()
    {
        if (!isBezig) return;
        isBezig = false;

        Debug.Log("AI is dood! Speler wint de ronde.");
        spelerWins++;
        StopAI();

        if (statusTekst != null)
        {
            statusTekst.text = $"Ronde {huidigeRonde} Gewonnen!\nKlik voor de volgende ronde!";
            statusTekst.color = Color.green;
        }

        if (gameAudioSource != null && winRondeSound != null) gameAudioSource.PlayOneShot(winRondeSound);

        OnRondeGewonnen?.Invoke();
        CheckEindeSpel();

        // Laat het menu weer zien zodat de speler kan klikken (ook na de laatste ronde)
        if (startMenuCanvas != null)
        {
            startMenuCanvas.SetActive(true);
            // Stop de muziek tussen de rondes in
            if (bgmAudioSource != null) bgmAudioSource.Stop();
        }
    }

    private void SpelerVerliest()
    {
        if (!isBezig) return;
        isBezig = false;

        Debug.Log("Speler is dood! AI wint de ronde.");
        aiWins++;
        StopAI();

        if (statusTekst != null)
        {
            statusTekst.text = $"Ronde {huidigeRonde} Verloren...\nKlik voor de volgende ronde.";
            statusTekst.color = Color.red;
        }

        if (gameAudioSource != null && loseRondeSound != null) gameAudioSource.PlayOneShot(loseRondeSound);

        OnRondeVerloren?.Invoke();
        CheckEindeSpel();

        // Laat het menu weer zien zodat de speler kan klikken (ook na de laatste ronde)
        if (startMenuCanvas != null)
        {
            startMenuCanvas.SetActive(true);
            // Stop de muziek tussen de rondes in
            if (bgmAudioSource != null) bgmAudioSource.Stop();
        }
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
                if (statusTekst != null) 
                {
                    statusTekst.text = $"KAMPIOEN!\nJe won met {spelerWins}-{aiWins}!";
                    statusTekst.color = Color.yellow;
                }
                
                if (gameAudioSource != null && winGameSound != null) gameAudioSource.PlayOneShot(winGameSound);
                if (bgmAudioSource != null) bgmAudioSource.Stop();
                
                OnSpelGewonnen?.Invoke();
            }
            else
            {
                Debug.Log($"AI wint het spel met {aiWins}-{spelerWins}!");
                if (statusTekst != null) 
                {
                    statusTekst.text = $"GAME OVER!\nJe verloor met {aiWins}-{spelerWins}.";
                    statusTekst.color = Color.red;
                }
                
                if (gameAudioSource != null && loseGameSound != null) gameAudioSource.PlayOneShot(loseGameSound);
                if (bgmAudioSource != null) bgmAudioSource.Stop();
                
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
