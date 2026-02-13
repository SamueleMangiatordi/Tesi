using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[RequireComponent(typeof(XRGrabInteractable))]
public class ExtinguisherSprayer : MonoBehaviour
{
    [Header("References")]
    public Transform nozzle;
    public ParticleSystem sprayParticles;

    [Header("Optional Visual: Safety Pin Object")]
    [Tooltip("Se hai un GameObject che rappresenta la sicura, assegnalo qui per nasconderlo quando viene rimossa.")]
    [SerializeField] private GameObject safetyPinVisual;

    [Header("Safety Pin")]
    public bool requireSafetyPin = true;
    public bool safetyPinRemoved = false;
    public KeyCode safetyPinKey = KeyCode.P;

    [Header("Spray Settings")]
    public float sprayRange = 5f;
    public float sprayRadius = 0.15f;
    public float extinguishRate = 1.0f;
    public LayerMask hitMask = ~0;

    [Header("Charge")]
    public float maxChargeSeconds = 8f;
    public float chargeRemaining;

    private XRGrabInteractable grab;
    private bool spraying;
    private bool emptyNotified;

    private ScenarioFlowManager flow;

    private void Awake()
    {
        grab = GetComponent<XRGrabInteractable>();
        flow = FindObjectOfType<ScenarioFlowManager>();

        chargeRemaining = maxChargeSeconds;

        if (sprayParticles != null)
            sprayParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        ApplySafetyPinVisual();
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
        // --- Rimozione sicura da tastiera ---
        if (requireSafetyPin && !safetyPinRemoved)
        {
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null && safetyPinKey == KeyCode.P && Keyboard.current.pKey.wasPressedThisFrame)
                RemoveSafetyPin();
#else
            if (Input.GetKeyDown(safetyPinKey))
                RemoveSafetyPin();
#endif
        }

        // --- Logica spray ---
        if (!spraying) return;

        if (chargeRemaining <= 0f)
        {
            ForceStopSpray();
            NotifyEmptyOnce();
            return;
        }

        chargeRemaining -= Time.deltaTime;

        if (chargeRemaining <= 0f)
        {
            ForceStopSpray();
            NotifyEmptyOnce();
            return;
        }

        if (nozzle == null) return;

        Ray ray = new Ray(nozzle.position, nozzle.forward);
        if (Physics.SphereCast(ray, sprayRadius, out RaycastHit hit, sprayRange, hitMask, QueryTriggerInteraction.Collide))
        {
            var fire = hit.collider.GetComponentInParent<FireTarget>();
            if (fire != null)
            {
      
                float mult = 1f;
                
                fire.ApplyExtinguish(extinguishRate * mult * Time.deltaTime);

            }

        }
    }

    private void OnSelectEntered(SelectEnterEventArgs args)
    {
        if (flow == null) flow = FindObjectOfType<ScenarioFlowManager>();
        flow?.OnExtinguisherGrabbed();
    }

    private void OnSelectExited(SelectExitEventArgs args)
    {
        if (spraying) StopSpray();
    }

    private void OnActivated(ActivateEventArgs args) => StartSpray();
    private void OnDeactivated(DeactivateEventArgs args) => StopSpray();

    public void ResetExtinguisher()
    {
        ForceStopSpray();
        chargeRemaining = maxChargeSeconds;
        emptyNotified = false;

        // reset sicura
        safetyPinRemoved = false;
        ApplySafetyPinVisual();
    }

    public void ForceStopSpray()
    {
        spraying = false;
        if (sprayParticles != null)
            sprayParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    public void StartSpray()
    {
        if (requireSafetyPin && !safetyPinRemoved)
        {
            if (ExperimentSettings.FeedbackOn)
                FeedbackUI.Instance?.ShowTemp("Rimuovi la sicura prima di spruzzare", 2.0f);
            return;
        }

        if (chargeRemaining <= 0f)
        {
            NotifyEmptyOnce();
            return;
        }

        if (flow == null) flow = FindObjectOfType<ScenarioFlowManager>();
        if (flow != null && !flow.CanStartSpray())
            return;

        spraying = true;

        if (sprayParticles != null)
        {
            sprayParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            sprayParticles.Play(true);
        }

        flow?.OnSprayStart();
    }


    public void StopSpray()
    {
        if (!spraying) return;

        spraying = false;

        if (sprayParticles != null)
            sprayParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        flow?.OnSprayStop();
    }


    private void NotifyEmptyOnce()
    {
        if (emptyNotified) return;
        emptyNotified = true;

        if (flow == null) flow = FindObjectOfType<ScenarioFlowManager>();
        flow?.OnExtinguisherEmpty();
    }

    public void RemoveSafetyPin()
    {
        // evita doppi trigger
        if (safetyPinRemoved) return;

        safetyPinRemoved = true;
        ApplySafetyPinVisual();   // deve nascondere la sicura visiva

        // sicurezza: se flow non è assegnato, recuperalo
        if (flow == null)
            flow = FindObjectOfType<ScenarioFlowManager>();

        if (flow != null)
            ExperimentFileLogger.MarkPin(flow.SessionElapsed);

        // Solo se feedback ON
        if (ExperimentSettings.FeedbackOn)
            FeedbackUI.Instance?.ShowTemp("Sicura rimossa", 1.5f);

        // debug utile: conferma che questa è la stessa istanza che spruzza
        Debug.Log($"[PIN] Removed on: {gameObject.name} id={GetInstanceID()} pinRemoved={safetyPinRemoved}");

        ConsoleLogger.Log("safety_pin_removed");
    }


    private void ApplySafetyPinVisual()
    {
        if (safetyPinVisual != null)
            safetyPinVisual.SetActive(!safetyPinRemoved);
    }
}
