using UnityEngine;
using TMPro;

public class RoundResultUI : MonoBehaviour
{
    public static RoundResultUI Instance { get; private set; }

    [Header("UI References")]
    public CanvasGroup group;
    public TMP_Text titleText;
    public TMP_Text bodyText;

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
        Show(title, body);
    }

    public void ShowFailed(float timeSeconds, string reason)
    {
        string title = "FALLITO";
        string body = $"Tempo impiegato: {timeSeconds:0.00}s\nMotivo: {reason}";
        if (showRestartHint) body += $"\n\n{restartHint}";
        Show(title, body);
    }

    public void Show(string title, string body)
    {
        HideAllSessionHUDs();

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

            // 1) Disabilita lo script (così Update non aggiorna più)
            hud.enabled = false;

            // 2) Nascondi direttamente il TMP che usa (anche se NON è figlio)
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
