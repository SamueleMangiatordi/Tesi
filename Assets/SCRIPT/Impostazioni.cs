using UnityEngine;

public class ExperimentSettings : MonoBehaviour
{
    public static ExperimentSettings Instance { get; private set; }

    [Header("Experiment Condition")]
    public bool feedbackOn = true;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[ExperimentSettings] Duplicate found -> destroying new one.");
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        Debug.Log($"[ExperimentSettings] Awake. feedbackOn={feedbackOn}");
    }

    public static bool FeedbackOn => Instance != null && Instance.feedbackOn;
}
