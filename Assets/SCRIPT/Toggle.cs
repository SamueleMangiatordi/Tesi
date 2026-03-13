using UnityEngine;
using UnityEngine.UI; // Necessario per il Toggle

[RequireComponent(typeof(Toggle))]
public class ToggleAiutiManager : MonoBehaviour
{
    private Toggle mioToggle;

    private void Start()
    {
        mioToggle = GetComponent<Toggle>();

        // 1. All'avvio della scena, si sincronizza con il Singleton sopravvissuto
        if (ExperimentSettings.Instance != null)
        {
            mioToggle.SetIsOnWithoutNotify(ExperimentSettings.FeedbackOn);
        }

        // 2. Si collega VIA CODICE all'evento del click, puntando sempre al Singleton vivo!
        mioToggle.onValueChanged.AddListener(AggiornaImpostazioni);
    }

    private void AggiornaImpostazioni(bool attivato)
    {
        if (ExperimentSettings.Instance != null)
        {
            ExperimentSettings.Instance.SetFeedbackOn(attivato);
        }
    }

    private void OnDestroy()
    {
        // Buona pratica: scollega l'evento quando l'oggetto viene distrutto
        if (mioToggle != null)
        {
            mioToggle.onValueChanged.RemoveListener(AggiornaImpostazioni);
        }
    }
}