using UnityEngine;

public class FireTarget : MonoBehaviour
{
    [Header("Fire Health")]
    public float maxFireHealth = 5f;
    public float currentFireHealth = 0f;

    [Header("Visuals")]
    public ParticleSystem fireParticles;
    [Tooltip("Opzionale: un GameObject da attivare/disattivare insieme al fuoco (es. il cube/mesh del fuoco)")]
    public GameObject fireVisualRoot;

    private bool isBurning;
    private bool extinguishNotified;
    private ScenarioFlowManager flow;

    private void Awake()
    {
        flow = FindObjectOfType<ScenarioFlowManager>();

        // Stato iniziale coerente con currentFireHealth
        if (currentFireHealth > 0f) Ignite();
        else ExtinguishImmediateForIdle();
    }

    public bool IsBurning => isBurning;

    public void Ignite()
    {
        extinguishNotified = false;

        currentFireHealth = Mathf.Max(0.01f, maxFireHealth);
        isBurning = true;

        if (fireVisualRoot != null) fireVisualRoot.SetActive(true);

        if (fireParticles != null)
        {
            fireParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            fireParticles.Play();
        }
    }

    public void ApplyExtinguish(float amount)
    {
        if (!isBurning) return;
        if (amount <= 0f) return;

        currentFireHealth -= amount;

        if (currentFireHealth <= 0f)
        {
            currentFireHealth = 0f;
            Extinguish();
        }
    }

    private void Extinguish()
    {
        if (!isBurning) return;

        isBurning = false;

        if (fireParticles != null)
            fireParticles.Stop(true, ParticleSystemStopBehavior.StopEmitting);

        if (fireVisualRoot != null)
            fireVisualRoot.SetActive(false);

        // Notifica una sola volta
        if (!extinguishNotified)
        {
            extinguishNotified = true;

            if (flow == null) flow = FindObjectOfType<ScenarioFlowManager>();
            flow?.OnFireExtinguished();
        }
    }

    // Usato dal manager per la fase "Preparazione"/reset
    public void ExtinguishImmediateForIdle()
    {
        isBurning = false;
        extinguishNotified = false;
        currentFireHealth = 0f;

        if (fireParticles != null)
            fireParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        if (fireVisualRoot != null)
            fireVisualRoot.SetActive(false);
    }
}
