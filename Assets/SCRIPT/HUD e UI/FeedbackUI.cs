using UnityEngine;
using TMPro;

public class FeedbackUI : MonoBehaviour
{
    public static FeedbackUI Instance { get; private set; }

    [Header("UI")]
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
        text.gameObject.SetActive(true);
        text.text = msg;
    }

    public void Clear()
    {
        if (text == null) return;
        text.text = "";
        text.gameObject.SetActive(true);
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
