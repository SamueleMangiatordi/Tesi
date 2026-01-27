using UnityEngine;

public class ExperimentSettings : MonoBehaviour
{
    public static ExperimentSettings Instance { get; private set; }

    [Header("Experiment Condition")]
    [SerializeField] private bool feedbackOn = true;

    public static bool FeedbackOn => Instance != null && Instance.feedbackOn;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            // Importantissimo: se arrivi da una "scena impostazioni" con un duplicato,
            // copia il valore nel singleton vero e poi distruggi il duplicato.
            Instance.feedbackOn = feedbackOn;
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        Debug.Log($"[ExperimentSettings] Awake. feedbackOn={feedbackOn}");
    }

    public void SetFeedbackOn(bool on)
    {
        feedbackOn = on;
        Debug.Log($"[ExperimentSettings] SetFeedbackOn -> {feedbackOn}");
    }

    public static void SetFeedback(bool on)
    {
        if (Instance == null)
        {
            Debug.LogWarning("[ExperimentSettings] SetFeedback called but Instance is null.");
            return;
        }

        Instance.SetFeedbackOn(on);
    }
}
