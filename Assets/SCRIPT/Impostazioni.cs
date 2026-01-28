using UnityEngine;

public class ExperimentSettings : MonoBehaviour
{
    public static ExperimentSettings Instance { get; private set; }

    [Header("Experiment Condition")]
    [SerializeField] private bool feedbackOn = true;
    [Tooltip("Se true, salva CSV/JSON su disco. La scelta viene letta SOLO all'inizio del tentativo.")]
    [SerializeField] private bool fileLoggingOn = true;

    public static bool FeedbackOn => Instance != null && Instance.feedbackOn;
    public static bool FileLoggingOn => Instance != null && Instance.fileLoggingOn;


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

    public void SetFileLoggingOn(bool on)
    {
        fileLoggingOn = on;
        Debug.Log($"[ExperimentSettings] SetFileLoggingOn -> {fileLoggingOn}");
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

    public static void SetFileLogging(bool on)
    {
        if (Instance == null) return;
        Instance.SetFileLoggingOn(on);
    }
}
