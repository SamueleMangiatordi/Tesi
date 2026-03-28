using UnityEngine;
using System.Collections;

public class SafetyPinFeedback : MonoBehaviour
{
    private MeshRenderer pinRenderer;
    private Color originalEmissionColor;
    private Coroutine currentPulseCoroutine;

    // Colore dell'impulso (ad esempio, un giallo brillante)
    public Color pulseColor = Color.yellow;
    // Velocità dell'impulso (durata in secondi)
    public float pulseDuration = 0.5f;

    void Awake()
    {
        pinRenderer = GetComponentInChildren<MeshRenderer>();        // Memorizza il colore di emissione originale per poterlo ripristinare
        originalEmissionColor = pinRenderer.material.GetColor("_EmissionColor");
    }

    // Metodo pubblico per avviare l'impulso
    public void StartPulse()
    {
        // Se c'è già un impulso in corso, lo fermiamo per farne partire uno nuovo
        if (currentPulseCoroutine != null)
        {
            StopCoroutine(currentPulseCoroutine);
        }
        currentPulseCoroutine = StartCoroutine(PulseCoroutine());
    }

    // Il "cuore" dell'impulso: una Coroutine che gestisce il tempo
    IEnumerator PulseCoroutine()
    {
        float timer = 0f;

        // Fase 1: Scurisci rapidamente l'emissione (opzionale, per un effetto più netto)
        pinRenderer.material.SetColor("_EmissionColor", Color.black);
        yield return new WaitForSeconds(0.05f); // Un breve lampo di buio

        // Fase 2: Fai brillare l'emissione fino al colore dell'impulso
        while (timer < pulseDuration / 2)
        {
            timer += Time.deltaTime;
            // Calcola il colore intermedio usando Mathf.Lerp
            Color lerpedColor = Color.Lerp(Color.black, pulseColor, timer / (pulseDuration / 2));
            pinRenderer.material.SetColor("_EmissionColor", lerpedColor);
            yield return null; // Attende il prossimo frame
        }

        // Fase 3: Mantieni il colore dell'impulso per un breve istante
        yield return new WaitForSeconds(0.1f);

        // Fase 4: Fai svanire l'emissione tornando al colore originale
        timer = 0f;
        while (timer < pulseDuration / 2)
        {
            timer += Time.deltaTime;
            Color lerpedColor = Color.Lerp(pulseColor, originalEmissionColor, timer / (pulseDuration / 2));
            pinRenderer.material.SetColor("_EmissionColor", lerpedColor);
            yield return null; // Attende il prossimo frame
        }

        // Assicurati che il colore finale sia esattamente quello originale
        pinRenderer.material.SetColor("_EmissionColor", originalEmissionColor);
        currentPulseCoroutine = null;
    }
}