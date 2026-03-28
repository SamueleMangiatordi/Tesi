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

    // Definiamo una chiave per i PlayerPrefs (come hai fatto per l'audio)
    private const string FeedbackPrefKey = "FeedbackOnPref";

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // --- CARICAMENTO DEL SALVATAGGIO ---
        // Leggiamo l'int dai PlayerPrefs. Se non esiste (es. prima volta che apri il gioco), 
        // impostiamo il default a 1 (cioè true).
        int savedFeedback = PlayerPrefs.GetInt(FeedbackPrefKey, 1);

        // Convertiamo l'int in bool: se è 1 diventa true, se è 0 diventa false.
        feedbackOn = (savedFeedback == 1);

        Debug.Log($"[ExperimentSettings] Awake. feedbackOn={feedbackOn}");
    }

    public void SetFeedbackOn(bool on)
    {
        feedbackOn = on;

        // --- SALVATAGGIO DELLA SCELTA ---
        // Convertiamo il bool in int: se 'on' è true salviamo 1, altrimenti 0.
        PlayerPrefs.SetInt(FeedbackPrefKey, on ? 1 : 0);
        PlayerPrefs.Save(); // Assicuriamoci che venga scritto su disco

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