using UnityEngine;
using UnityEngine.UI; // Necessario per interagire con lo Slider

[RequireComponent(typeof(Slider))]
public class SliderSuonoManager : MonoBehaviour
{
    private Slider mioSlider;

    private void Start()
    {
        mioSlider = GetComponent<Slider>();

        // 1. All'avvio, sincronizza la levetta con il volume salvato nel Singleton
        if (AudioSettingsManager.Instance != null)
        {
            // Usiamo SetValueWithoutNotify per non far scattare l'evento per sbaglio
            mioSlider.SetValueWithoutNotify(AudioSettingsManager.Instance.CurrentSfx);
        }

        // 2. Collega l'evento via codice: quando muovi la levetta, aggiorna il Singleton!
        mioSlider.onValueChanged.AddListener(AggiornaVolume);
    }

    private void AggiornaVolume(float valore)
    {
        if (AudioSettingsManager.Instance != null)
        {
            AudioSettingsManager.Instance.SetSfxVolume(valore);
        }
    }

    private void OnDestroy()
    {
        // Buona pratica: scollega l'evento quando l'oggetto viene distrutto
        if (mioSlider != null)
        {
            mioSlider.onValueChanged.RemoveListener(AggiornaVolume);
        }
    }
}