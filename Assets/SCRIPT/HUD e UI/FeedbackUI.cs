using UnityEngine;
using TMPro;

public class FeedbackUI : MonoBehaviour
{
    public static FeedbackUI Instance { get; private set; }

    [Header("UI")]
    [Tooltip("Trascina qui l'AiutiContainer")]
    public GameObject container; // Il contenitore padre (sfondo + testo)
    public TMP_Text text;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (text == null)
            text = GetComponentInChildren<TMP_Text>(true);

        Clear();
    }

    public void Show(string msg)
    {
        if (text == null) return;

        text.text = msg;

        // Attiva il contenitore intero
        if (container != null)
            container.SetActive(true);
        else
            text.gameObject.SetActive(true); // Fallback di sicurezza
    }

    public void Clear()
    {
        if (text == null) return;

        text.text = "";

        // Disattiva il contenitore in modo che scompaia anche lo sfondo
        if (container != null)
            container.SetActive(false);
        else
            text.gameObject.SetActive(false); // Corretto da true a false
    }

    private void ClearLater() => Clear();

    public void ShowTemp(string msg, float seconds = 3f)
    {
        Show(msg);
        CancelInvoke(nameof(ClearLater));
        Invoke(nameof(ClearLater), seconds);
    }

    // Sempre visibile (indipendente da Feedback ON/OFF)
    public void ShowTempAlways(string msg, float seconds = 3f)
    {
        Show(msg);
        CancelInvoke(nameof(ClearLater));
        Invoke(nameof(ClearLater), seconds);
    }
}