using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[RequireComponent(typeof(XRGrabInteractable))]
public class ExtinguisherSprayer : MonoBehaviour
{
    [Header("References")]
    public Transform nozzle;
    public ParticleSystem sprayParticles;

    [Header("Spray Settings")]
    public float sprayRange = 5f;
    public float sprayRadius = 0.15f;
    public float extinguishRate = 1.0f;
    public LayerMask hitMask = ~0;

    [Header("Charge")]
    public float maxChargeSeconds = 8f;
    public float chargeRemaining;

    [Header("PASS - Light rules")]
    public bool requirePullPin = true;

    [Header("Sweep (BONUS, non penalità) - MOVIMENTO LATERALE")]
    public bool requireSweep = true;
    public float sweepMinLateralMeters = 0.05f;
    public float sweepWindowSec = 2.0f;
    public float sweepBonusMultiplier = 1.4f;

    [Header("Feedback (anti spam)")]
    public float feedbackCooldown = 0.8f;
    private float nextFeedbackTime = 0f;

    [Header("State (runtime)")]
    public bool pinPulled = false;
    public bool debugLogs = false;

    private XRGrabInteractable grab;
    private bool spraying;
    private ScenarioFlowManager flow;

    // Sweep tracking
    private bool sweepOk;
    private float sweepTimer;
    private Vector3 sweepStartPos;
    private float sweepLateralMax;

    // no spam
    private bool sweepBonusLoggedThisSpray = false;
    private bool lastSweepOk = false;

    // ✅ per non ripetere “scarico”
    private bool emptyNotified = false;

    private void Awake()
    {
        grab = GetComponent<XRGrabInteractable>();
        flow = FindObjectOfType<ScenarioFlowManager>();

        chargeRemaining = maxChargeSeconds;

        if (sprayParticles != null)
            sprayParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    private void OnEnable()
    {
        grab.activated.AddListener(OnActivated);
        grab.deactivated.AddListener(OnDeactivated);
        grab.selectEntered.AddListener(OnSelectEntered);
        grab.selectExited.AddListener(OnSelectExited);
    }

    private void OnDisable()
    {
        grab.activated.RemoveListener(OnActivated);
        grab.deactivated.RemoveListener(OnDeactivated);
        grab.selectEntered.RemoveListener(OnSelectEntered);
        grab.selectExited.RemoveListener(OnSelectExited);
    }

    private void Update()
    {
        // Pull Pin (tastiera per ora)
        if (IsHeld() && requirePullPin && !pinPulled)
        {
#if ENABLE_INPUT_SYSTEM
            if (UnityEngine.InputSystem.Keyboard.current != null &&
                UnityEngine.InputSystem.Keyboard.current.pKey.wasPressedThisFrame)
            {
                PullPin();
            }
#else
            if (Input.GetKeyDown(KeyCode.P))
                PullPin();
#endif
        }

        if (!spraying) return;

        if (chargeRemaining <= 0f)
        {
            StopSpray();

            if (!emptyNotified)
            {
                emptyNotified = true;
                ConsoleLogger.Log("extinguisher_empty");

                if (ExperimentSettings.FeedbackOn)
                    FeedbackUI.Instance?.ShowTemp("Estintore scarico", 3f);
            }
            return;
        }

        chargeRemaining -= Time.deltaTime;
        if (nozzle == null) return;

        UpdateSweepTracking();

        Ray ray = new Ray(nozzle.position, nozzle.forward);
        if (Physics.SphereCast(ray, sprayRadius, out RaycastHit hit, sprayRange, hitMask, QueryTriggerInteraction.Collide))
        {
            var fire = hit.collider.GetComponentInParent<FireTarget>();
            if (fire != null)
            {
                float multiplier = 1f;

                // Sweep bonus (non penalità)
                if (requireSweep && sweepOk)
                    multiplier *= sweepBonusMultiplier;

                fire.ApplyExtinguish(extinguishRate * multiplier * Time.deltaTime);

                if (debugLogs)
                    Debug.Log($"SweepOk={sweepOk} mult={multiplier:0.00}");
            }
        }
    }

    private void OnSelectEntered(SelectEnterEventArgs args)
    {
        if (flow == null) flow = FindObjectOfType<ScenarioFlowManager>();
        flow?.OnExtinguisherGrabbed();

        ConsoleLogger.Log("extinguisher_grabbed");
        ResetPassRuntime();
    }

    private void OnSelectExited(SelectExitEventArgs args)
    {
        if (spraying) StopSpray();
    }

    private void OnActivated(ActivateEventArgs args) => StartSpray();
    private void OnDeactivated(DeactivateEventArgs args) => StopSpray();

    public void ResetExtinguisher()
    {
        StopSpray();
        chargeRemaining = maxChargeSeconds;
        pinPulled = false;

        emptyNotified = false; // ✅ reset notifica scarico

        ResetPassRuntime();
        ConsoleLogger.Log("extinguisher_reset", $"charge={chargeRemaining:0.00}s");
    }

    public void PullPin()
    {
        pinPulled = true;
        ConsoleLogger.Log("pass_pull_pin");

        if (ExperimentSettings.FeedbackOn)
            FeedbackUI.Instance?.ShowTemp("Pin rimosso: puoi spruzzare", 2f);
    }

    private void ResetPassRuntime()
    {
        sweepTimer = 0f;
        sweepLateralMax = 0f;

        sweepOk = !requireSweep;
        lastSweepOk = sweepOk;

        sweepBonusLoggedThisSpray = false;

        if (nozzle != null)
            sweepStartPos = nozzle.position;
    }

    public void StartSpray()
    {
        if (requirePullPin && !pinPulled)
        {
            ConsoleLogger.Warn("error_pin_not_pulled");

            if (ExperimentSettings.FeedbackOn && Time.time >= nextFeedbackTime)
            {
                FeedbackUI.Instance?.ShowTemp("Prima: Pull Pin (premi P)", feedbackCooldown);
                nextFeedbackTime = Time.time + feedbackCooldown;
            }
            return;
        }

        if (chargeRemaining <= 0f)
        {
            ConsoleLogger.Warn("spray_blocked_empty");

            if (!emptyNotified)
            {
                emptyNotified = true;
                if (ExperimentSettings.FeedbackOn)
                    FeedbackUI.Instance?.ShowTemp("Estintore scarico", 3f);
            }
            return;
        }

        if (spraying) return;

        spraying = true;
        emptyNotified = false; // ✅ se spruzzi di nuovo, resetto la possibilità di notificare

        if (sprayParticles != null && !sprayParticles.isPlaying)
            sprayParticles.Play();

        ResetPassRuntime();

        if (flow == null) flow = FindObjectOfType<ScenarioFlowManager>();
        flow?.OnSprayStart();

        ConsoleLogger.Log("spray_start", $"charge={chargeRemaining:0.00}s");
    }

    public void StopSpray()
    {
        if (!spraying) return;

        spraying = false;

        if (sprayParticles != null)
            sprayParticles.Stop(true, ParticleSystemStopBehavior.StopEmitting);

        if (flow == null) flow = FindObjectOfType<ScenarioFlowManager>();
        flow?.OnSprayStop();

        ConsoleLogger.Log("spray_stop", $"charge={chargeRemaining:0.00}s");
    }

    private bool IsHeld() => grab != null && grab.isSelected;

    private void UpdateSweepTracking()
    {
        if (!requireSweep || nozzle == null)
        {
            sweepOk = true;
            return;
        }

        sweepTimer += Time.deltaTime;

        Vector3 delta = nozzle.position - sweepStartPos;
        float lateral = Mathf.Abs(Vector3.Dot(delta, nozzle.right));
        if (lateral > sweepLateralMax) sweepLateralMax = lateral;

        if (sweepTimer >= sweepWindowSec)
        {
            sweepOk = sweepLateralMax >= sweepMinLateralMeters;

            // log sweep_bonus una sola volta quando diventa true
            if (!lastSweepOk && sweepOk && !sweepBonusLoggedThisSpray)
            {
                ConsoleLogger.Log("sweep_bonus", $"lateralMax={sweepLateralMax:0.00}m");
                sweepBonusLoggedThisSpray = true;
            }

            lastSweepOk = sweepOk;

            sweepTimer = 0f;
            sweepStartPos = nozzle.position;
            sweepLateralMax = 0f;
        }
    }
}
