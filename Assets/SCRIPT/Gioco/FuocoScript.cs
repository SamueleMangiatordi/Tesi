using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public enum FireClass
{
    A,
    B,
    C,
    Elettrico
}

public class FireTarget : MonoBehaviour
{
    [Header("Fire Class")]
    public FireClass fireClass; // Classe del fuoco (A, B, C, Elettrico)

    // Liste degli estintori per ogni tipo di incendio
    public List<ExtinguisherType> recommendedExtinguishers = new List<ExtinguisherType>();
    public List<ExtinguisherType> secondaryExtinguishers = new List<ExtinguisherType>();
    public List<ExtinguisherType> notRecommendedExtinguishers = new List<ExtinguisherType>();

    [Header("Fire Health")]
    public float maxFireHealth = 5f;
    public float currentFireHealth = 0f;

    [Header("Visuals - Active Fire")]
    public GameObject fireVisualRoot; // es: VFX_Fire_Active

    [Header("Visuals - Aftermath (smoke/embers)")]
    public GameObject aftermathVisualRoot; // es: VFX_Fire_Aftermath
    //public float aftermathDuration = 6f;

    private ParticleSystem[] firePS;
    private ParticleSystem[] aftermathPS;

    private bool isBurning;
    private bool extinguishNotified;
    private ScenarioFlowManager flow;
    private Coroutine aftermathRoutine;

    private void Awake()
    {
        flow = FindObjectOfType<ScenarioFlowManager>();

        // Prende TUTTI i particle system sotto i root, anche se inattivi
        if (fireVisualRoot != null)
            firePS = fireVisualRoot.GetComponentsInChildren<ParticleSystem>(true);

        if (aftermathVisualRoot != null)
            aftermathPS = aftermathVisualRoot.GetComponentsInChildren<ParticleSystem>(true);

        if (currentFireHealth > 0f) Ignite();
        else ExtinguishImmediateForIdle();
    }

    public bool IsBurning => isBurning;

    public void Ignite()
    {
        extinguishNotified = false;
        currentFireHealth = Mathf.Max(0.01f, maxFireHealth);
        isBurning = true;

        // stop aftermath
        if (aftermathRoutine != null) StopCoroutine(aftermathRoutine);
        if (aftermathVisualRoot != null) aftermathVisualRoot.SetActive(false);
        StopAndClear(aftermathPS);

        // start fire
        if (fireVisualRoot != null) fireVisualRoot.SetActive(true);
        PlayAll(firePS);
    }

    public void ApplyExtinguish(float amount, ExtinguisherType typeUsed)
    {
        if (!isBurning) return;

        // Controlla se l'estintore usato può spegnere questo fuoco
        if (!recommendedExtinguishers.Contains(typeUsed) && !secondaryExtinguishers.Contains(typeUsed))
        {
            // Estintore inefficace, esci senza modificare la salute del fuoco
            Debug.Log("Estintore non efficace per questo fuoco!");
            return;
        }

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

        // stop active fire
        StopEmitting(firePS);
        if (fireVisualRoot != null) fireVisualRoot.SetActive(false);

        // start aftermath
        if (aftermathVisualRoot != null) aftermathVisualRoot.SetActive(true);
        PlayAll(aftermathPS);

        //if (aftermathDuration > 0f)
            //aftermathRoutine = StartCoroutine(StopAftermathAfterSeconds(aftermathDuration));

        if (!extinguishNotified)
        {
            extinguishNotified = true;
            if (flow == null) flow = FindObjectOfType<ScenarioFlowManager>();
            flow?.OnFireExtinguished();
        }
    }

    public void ExtinguishImmediateForIdle()
    {
        isBurning = false;
        extinguishNotified = false;
        currentFireHealth = 0f;

        if (aftermathRoutine != null) StopCoroutine(aftermathRoutine);

        StopAndClear(firePS);
        if (fireVisualRoot != null) fireVisualRoot.SetActive(false);

        StopAndClear(aftermathPS);
        if (aftermathVisualRoot != null) aftermathVisualRoot.SetActive(false);
    }

    private IEnumerator StopAftermathAfterSeconds(float s)
    {
        // se vuoi che continui anche quando Time.timeScale=0 usa WaitForSecondsRealtime
        yield return new WaitForSeconds(s);

        StopEmitting(aftermathPS);
        if (aftermathVisualRoot != null) aftermathVisualRoot.SetActive(false);
    }

    private void PlayAll(ParticleSystem[] list)
    {
        if (list == null) return;
        foreach (var ps in list)
            if (ps != null)
            {
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                ps.Play(true);
            }
    }

    private void StopEmitting(ParticleSystem[] list)
    {
        if (list == null) return;
        foreach (var ps in list)
            if (ps != null)
                ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
    }

    private void StopAndClear(ParticleSystem[] list)
    {
        if (list == null) return;
        foreach (var ps in list)
            if (ps != null)
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    // Metodo per inizializzare la lista degli estintori in base alla classe
    public void SetFireClass(FireClass fireClass)
    {
        this.fireClass = fireClass;

        switch (fireClass)
        {
            case FireClass.A:
                recommendedExtinguishers = new List<ExtinguisherType> { ExtinguisherType.Acqua };
                secondaryExtinguishers = new List<ExtinguisherType> { ExtinguisherType.Schiuma, ExtinguisherType.Polvere };
                notRecommendedExtinguishers = new List<ExtinguisherType> { ExtinguisherType.CO2 };
                break;

            case FireClass.B:
                recommendedExtinguishers = new List<ExtinguisherType> { ExtinguisherType.Schiuma };
                secondaryExtinguishers = new List<ExtinguisherType> { ExtinguisherType.CO2, ExtinguisherType.Polvere };
                notRecommendedExtinguishers = new List<ExtinguisherType> { ExtinguisherType.Acqua };
                break;

            case FireClass.C:
                recommendedExtinguishers = new List<ExtinguisherType> { ExtinguisherType.Polvere };
                secondaryExtinguishers = new List<ExtinguisherType> { ExtinguisherType.CO2 };
                notRecommendedExtinguishers = new List<ExtinguisherType> { ExtinguisherType.Acqua, ExtinguisherType.Schiuma };
                break;

            case FireClass.Elettrico:
                recommendedExtinguishers = new List<ExtinguisherType> { ExtinguisherType.CO2 };
                secondaryExtinguishers = new List<ExtinguisherType>();
                notRecommendedExtinguishers = new List<ExtinguisherType> { ExtinguisherType.Acqua, ExtinguisherType.Schiuma, ExtinguisherType.Polvere };
                break;
        }
    }
}
