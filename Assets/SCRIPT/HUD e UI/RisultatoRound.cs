using UnityEngine;
using TMPro;

public class RoundResultUI : MonoBehaviour
{
    public static RoundResultUI Instance { get; private set; }

    [Header("UI References")]
    public CanvasGroup group;
    public TMP_Text titleText;
    public TMP_Text bodyText;

    // [MODIFICATO] Aggiunto un riferimento per nascondere l'intero HUD di gioco
    [Header("Riferimenti HUD Principale")]
    [Tooltip("L'intero GameObject 'HUD' contenente sfondi, testi di stato e aiuti.")]
    public GameObject interoHUDPrincipale; // Trascina qui l'oggetto 'HUD'

    [Header("Riferimenti Pausa")]
    [Tooltip("Il bottone fisico della pausa nell'HUD del giocatore")]
    public GameObject bottonePausaHUD;
    [Tooltip("Lo script che gestisce la logica della pausa")]
    public PauseManager pauseManager;

    [Header("Optional")]
    public bool showRestartHint = true;
    public string restartHint = "Premi R per riavviare";

    // Cache HUD trovati (anche se duplicati)
    private SessionHUD[] cachedHuds;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (group == null) group = GetComponentInChildren<CanvasGroup>(true);

        // Trova tutti i SessionHUD presenti (anche disattivi)
        cachedHuds = FindObjectsOfType<SessionHUD>(true);

        Hide();
    }

    public void ShowCompleted(float timeSeconds)
    {
        string title = "COMPLETATO";
        string body = $"Tempo impiegato: {timeSeconds:0.00}s";

        if (showRestartHint) body += $"\n\n{restartHint}";

        // Usa il percorso corretto di ExperimentFileLogger
        if (!string.IsNullOrEmpty(ExperimentFileLogger.PercorsoSalvataggi))
        {
            body += $"\n\n<size=70%><i>Dati salvati in:\n{ExperimentFileLogger.PercorsoSalvataggi}</i></size>";
        }

        Show(title, body);
    }

    public void ShowFailed(float timeSeconds, string reason)
    {
        string title = "FALLITO";
        string body = $"Tempo impiegato: {timeSeconds:0.00}s\nMotivo: {reason}";

        if (showRestartHint) body += $"\n\n{restartHint}";

        // Usa il percorso corretto di ExperimentFileLogger
        if (!string.IsNullOrEmpty(ExperimentFileLogger.PercorsoSalvataggi))
        {
            body += $"\n\n<size=70%><i>Dati sessione salvati in:\n{ExperimentFileLogger.PercorsoSalvataggi}</i></size>";
        }

        Show(title, body);
    }

    public void Show(string title, string body)
    {
        HideAllSessionHUDs();

        // Pulisci e nascondi il contenitore dei Feedback/Aiuti (Singleton)
        if (FeedbackUI.Instance != null)
        {
            FeedbackUI.Instance.Clear();
        }

        // [AGGIUNTO] Nascondi l'intero HUD principale (inclusi tutti i suoi sfondi e testi)
        if (interoHUDPrincipale != null)
            interoHUDPrincipale.SetActive(false);

        // Nascondi il bottone della pausa
        if (bottonePausaHUD != null)
            bottonePausaHUD.SetActive(false);

        // Blocca la logica della pausa (impedisce di premere il tasto o controller)
        if (pauseManager != null)
            pauseManager.canPause = false;

        if (titleText != null) titleText.text = title;
        if (bodyText != null) bodyText.text = body;

        if (group != null)
        {
            group.alpha = 1f;
            group.interactable = true;
            group.blocksRaycasts = true;
        }
        else
        {
            gameObject.SetActive(true);
        }
    }

    public void Hide()
    {
        ShowAllSessionHUDs();

        // [AGGIUNTO] Riattiva l'intero HUD principale (così ricompare all'inizio del round successivo)
        if (interoHUDPrincipale != null)
            interoHUDPrincipale.SetActive(true);

        // Riattiva il bottone della pausa (se nascondi i risultati)
        if (bottonePausaHUD != null)
            bottonePausaHUD.SetActive(true);

        // Sblocca la logica della pausa
        if (pauseManager != null)
            pauseManager.canPause = true;

        if (group != null)
        {
            group.alpha = 0f;
            group.interactable = false;
            group.blocksRaycasts = false;
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    private void HideAllSessionHUDs()
    {
        if (cachedHuds == null || cachedHuds.Length == 0)
            cachedHuds = FindObjectsOfType<SessionHUD>(true);

        foreach (var hud in cachedHuds)
        {
            if (hud == null) continue;

            // Disabilita lo script
            hud.enabled = false;

            // Nascondi direttamente il TMP che usa
            if (hud.statusText != null)
                hud.statusText.gameObject.SetActive(false);
            else
                hud.gameObject.SetActive(false);
        }
    }

    private void ShowAllSessionHUDs()
    {
        if (cachedHuds == null || cachedHuds.Length == 0)
            cachedHuds = FindObjectsOfType<SessionHUD>(true);

        foreach (var hud in cachedHuds)
        {
            if (hud == null) continue;

            hud.enabled = true;

            if (hud.statusText != null)
                hud.statusText.gameObject.SetActive(true);
            else
                hud.gameObject.SetActive(true);
        }
    }
}