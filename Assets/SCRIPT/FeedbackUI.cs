using UnityEngine;
using TMPro;

public class FeedbackUI : MonoBehaviour
{
    public static FeedbackUI Instance { get; private set; }

    [Header("UI")]
    public TMP_Text text; // puoi lasciarlo vuoto: auto-find

    private float lastShowTime;

    private void Awake()
    {
        Instance = this;

        if (text == null)
            text = GetComponentInChildren<TMP_Text>(true);

        if (text != null) text.text = "";
    }

    public void Show(string msg)
    {
        lastShowTime = Time.time;
        if (text != null) text.text = msg;
    }

    public void ShowTemp(string msg, float seconds = 3f)
    {
        Show(msg);
        CancelInvoke(nameof(ClearLater));
        Invoke(nameof(ClearLater), seconds);
    }

    private void ClearLater()
    {
        // cancella solo se non è stato aggiornato nel frattempo
        if (Time.time - lastShowTime >= 0.9f)
            Show("");
    }
}
