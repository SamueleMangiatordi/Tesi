using UnityEngine;

public class FireTarget : MonoBehaviour
{
    [Header("Fire Health")]
    public float maxFireHealth = 5f;
    public float currentFireHealth;

    [Header("Visuals")]
    public ParticleSystem fireParticles;
    public GameObject fireVisualRoot;

    private bool isBurning;
    private bool alreadyNotified;

    private ScenarioFlowManager flow;

    private void Awake()
    {
        flow = FindObjectOfType<ScenarioFlowManager>();
    }

    public void Ignite()
    {
        isBurning = true;
        alreadyNotified = false;
        currentFireHealth = maxFireHealth;

        if (fireVisualRoot != null) fireVisualRoot.SetActive(true);
        if (fireParticles != null)
        {
            fireParticles.Play(true);
        }
    }

    public void ExtinguishImmediateForIdle()
    {
        isBurning = false;
        alreadyNotified = true;
        currentFireHealth = 0f;

        if (fireParticles != null) fireParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        if (fireVisualRoot != null) fireVisualRoot.SetActive(false);
    }

    public void ApplyExtinguish(float amount)
    {
        if (!isBurning) return;

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

        if (fireParticles != null) fireParticles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        if (fireVisualRoot != null) fireVisualRoot.SetActive(false);

        if (!alreadyNotified)
        {
            alreadyNotified = true;

            if (flow == null) flow = FindObjectOfType<ScenarioFlowManager>();
            flow?.OnFireExtinguished();
        }
    }
}
