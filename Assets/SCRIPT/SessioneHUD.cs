using UnityEngine;
using TMPro;

public class SessionHUD : MonoBehaviour
{
    public ScenarioFlowManager flow;
    public TMP_Text statusText;

    private void Awake()
    {
        if (flow == null) flow = FindObjectOfType<ScenarioFlowManager>();
        if (statusText == null) statusText = GetComponentInChildren<TMP_Text>(true);
    }

    private void Update()
    {
        if (flow == null || statusText == null) return;

        string fb = ExperimentSettings.FeedbackOn ? "ON" : "OFF";

        if (flow.Preparing)
        {
            statusText.text = $"Stato: Preparazione | Incendio tra {flow.PreparationRemaining:0}s | Feedback: {fb}";
            return;
        }

        statusText.text = $"Stato: {flow.state} | t={flow.SessionElapsed:0.0}s | Feedback: {fb}";
    }
}
