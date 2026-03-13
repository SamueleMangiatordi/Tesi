using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class ScenarioFlowManager : MonoBehaviour
{
    public enum ScenarioState
    {
        Preparazione,
        Incendio,
        Allarme,
        Ripristino,
        Completato,
        Fallito
    }

    private enum ExpectedStep
    {
        AttendiIncendio,
        AttivaAllarme,
        PrendiEstintore,
        SpegniIncendio,
        SpegniAllarme,
        Fine
    }

    [Header("References (Scenario)")]
    public AlarmSwitch alarmSwitch;
    public FireTarget[] fireTargets;

    [Header("Extinguishers")]
    public ExtinguisherSprayer[] extinguishers; // metti qui i 4 estintori da Inspector
    public Transform[] extinguisherSpawnPoints; // opzionale: 4 spawnpoint (stessa lunghezza)

    private readonly Dictionary<ExtinguisherSprayer, (Vector3 pos, Quaternion rot)> extStartPose
    = new Dictionary<ExtinguisherSprayer, (Vector3, Quaternion)>();

    private ExtinguisherSprayer currentExt;


    [Header("UI Result (optional)")]
    public RoundResultUI roundResultUI; // se null usa RoundResultUI.Instance

    [Header("Freeze on End")]
    public bool freezeOnEnd = true;

    [Header("Player Reset")]
    public Transform xrOriginRoot;
    public Transform xrCamera;
    public Transform playerSpawnPoint;
    public GameObject[] disableDuringReset;

    [Header("Session Start")]
    public float fireStartDelaySeconds = 5f;
    public bool showStartCountdown = true;

    [Header("Restart (Keyboard)")]
    public bool allowKeyboardRestart = true;
    public KeyCode restartKey = KeyCode.R;

    [Header("Runtime")]
    public ScenarioState state = ScenarioState.Preparazione;

    /*public FireManager fireManager; // Riferimento al FireManager
    public GameObject fireTargetObject; // L'oggetto che rappresenta il fuoco nella scena

    private FireType currentFire;*/


    private float sessionStartTime;
    private bool sessionRunning;
    private float sessionEndTime = -1f;

    private float plannedIgnitionTime = -1f;

    private bool fireActive = false;
    private bool alarmOn = false;
    private FireTarget selectedFire; // Il fuoco selezionato e attivo

    private ExpectedStep expectedStep = ExpectedStep.AttendiIncendio;

    private bool isResetting = false;
    private bool attemptEnded = false;

    private bool extinguisherCollected = false;
    private string lastFailReason = "";


    private Coroutine flowRoutine;

    private float defaultFixedDeltaTime;

    public bool SessionRunning => sessionRunning;

    public float SessionElapsed
    {
        get
        {
            if (sessionRunning)
                return Time.time - sessionStartTime;

            if (sessionStartTime > 0f && sessionEndTime > 0f)
                return sessionEndTime - sessionStartTime;

            return 0f;
        }
    }

    public bool Preparing => !sessionRunning && plannedIgnitionTime > 0f && Time.time < plannedIgnitionTime;
    public float PreparationRemaining => Preparing ? Mathf.Max(0f, plannedIgnitionTime - Time.time) : 0f;

    private void Awake()
    {
        defaultFixedDeltaTime = Time.fixedDeltaTime;
    }

    private IEnumerator Start()
    {
        yield return null;
        yield return new WaitForEndOfFrame(); // Aspetta due frame per sicurezza


        CacheExtinguishersStartPose();

        RestartSession(isInitialStart: true);
    }

    private void Update()
    {
        if (!allowKeyboardRestart) return;

#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null)
        {
            bool pressed = restartKey switch
            {
                KeyCode.R => Keyboard.current.rKey.wasPressedThisFrame,
                KeyCode.Space => Keyboard.current.spaceKey.wasPressedThisFrame,
                KeyCode.Return => Keyboard.current.enterKey.wasPressedThisFrame,
                KeyCode.Escape => Keyboard.current.escapeKey.wasPressedThisFrame,
                _ => false
            };

            if (pressed) RestartSession(isInitialStart: false);
        }
#else
        if (Input.GetKeyDown(restartKey))
            RestartSession(isInitialStart: false);
#endif
    }

    private void CacheExtinguishersStartPose()
    {
        extStartPose.Clear();
        if (extinguishers == null) return;

        for (int i = 0; i < extinguishers.Length; i++)
        {
            var ext = extinguishers[i];
            if (ext == null) continue;

            Transform sp = (extinguisherSpawnPoints != null && i < extinguisherSpawnPoints.Length)
                ? extinguisherSpawnPoints[i]
                : null;

            // Imposta posizione e rotazione dall'oggetto spawn
            Vector3 p = sp != null ? sp.position : ext.transform.position;
            Quaternion r = sp != null ? sp.rotation : ext.transform.rotation;

            extStartPose[ext] = (p, r);

            // Imposta la posizione e rotazione finale dell'estintore
            ext.transform.SetPositionAndRotation(p, r);
        }
    }

    private RoundResultUI ResultUI => roundResultUI != null ? roundResultUI : RoundResultUI.Instance;

    private void FreezeGame(bool freeze)
    {
        if (!freezeOnEnd) return;

        if (freeze)
        {
            Time.timeScale = 0f;
            Time.fixedDeltaTime = 0f;
        }
        else
        {
            Time.timeScale = 1f;
            Time.fixedDeltaTime = defaultFixedDeltaTime;
        }
    }

    // ✅ FEEDBACK: solo se feedback ON
    private void ShowFeedback(string msg, float seconds = 2f)
    {
        if (!ExperimentSettings.FeedbackOn) return;
        FeedbackUI.Instance?.ShowTemp(msg, seconds);
    }

    // ✅ MESSAGGI GUIDA: SOLO se feedback ON (tu non li vuoi con feedback OFF)
    private void ShowGuidance(string msg, float seconds = 3f)
    {
        if (!ExperimentSettings.FeedbackOn) return;
        FeedbackUI.Instance?.ShowTempAlways(msg, seconds);
    }

    // ✅ COUNTDOWN: sempre visibile (anche con feedback OFF)
    private void ShowCountdown(string msg, float seconds = 1.05f)
    {
        FeedbackUI.Instance?.ShowTempAlways(msg, seconds);
    }

    private void EndAsFailed(string reason)
    {
        if (attemptEnded) return;

        attemptEnded = true;
        lastFailReason = reason;

        if (sessionRunning && sessionStartTime > 0f)
            sessionEndTime = Time.time;

        sessionRunning = false;
        state = ScenarioState.Fallito;

        ConsoleLogger.Log("attempt_failed", reason);

        ExperimentFileLogger.EndAttempt("failed", SessionElapsed, reason);


        // Pulisce eventuali testi rimasti
        FeedbackUI.Instance?.Clear();

        // UI finale sempre
        ResultUI?.ShowFailed(SessionElapsed, reason);

        StopAllSprays();

        FreezeGame(true);
    }

    private void EndAsCompleted()
    {
        if (attemptEnded) return;

        attemptEnded = true;

        sessionEndTime = Time.time;
        sessionRunning = false;
        state = ScenarioState.Completato;

        StopAllSprays();

        ConsoleLogger.Log("completed", $"total={SessionElapsed:0.00}s");

        ExperimentFileLogger.EndAttempt("completed", SessionElapsed);

        FeedbackUI.Instance?.Clear();

        ResultUI?.ShowCompleted(SessionElapsed);

        FreezeGame(true);
    }

    public void RestartSession(bool isInitialStart)
    {
        FreezeGame(false);
        ResultUI?.Hide();
        FeedbackUI.Instance?.Clear();

        if (flowRoutine != null) StopCoroutine(flowRoutine);
        flowRoutine = StartCoroutine(RestartFlowRoutine(isInitialStart));
    }

    private IEnumerator RestartFlowRoutine(bool isInitialStart)
    {
        isResetting = true;
        attemptEnded = false;

        sessionRunning = false;
        sessionStartTime = 0f;
        sessionEndTime = -1f;

        plannedIgnitionTime = -1f;

        fireActive = false;
        alarmOn = false;

        expectedStep = ExpectedStep.AttendiIncendio;

        extinguisherCollected = false;
        lastFailReason = "";

        state = ScenarioState.Preparazione;

        if (alarmSwitch != null)
            alarmSwitch.SetOn(false);

        if (fireTargets != null && fireTargets.Length > 0)
        {
            foreach (var fire in fireTargets)
            {
                if (fire != null)
                {
                    fire.ExtinguishImmediateForIdle(); // Spegni il fuoco prima di avviarne uno nuovo
                }
            }
        }

        yield return StartCoroutine(ResetRoutine());

        isResetting = false;

        // inizio log su file (un tentativo = una sessione)
        ExperimentFileLogger.BeginAttempt(ExperimentSettings.FeedbackOn, ExperimentSettings.FileLoggingOn);
        ConsoleLogger.Log(isInitialStart ? "start_session" : "restart_session");

        plannedIgnitionTime = Time.time + Mathf.Max(0f, fireStartDelaySeconds);

        // Questo lo mostriamo SOLO se feedback ON (tu hai detto solo countdown)
        ShowGuidance(isInitialStart ? "Sessione avviata: preparati..." : "Sessione riavviata: preparati...", 2.0f);

        // countdown sempre visibile
        if (showStartCountdown && fireStartDelaySeconds > 0.5f)
        {
            int seconds = Mathf.CeilToInt(fireStartDelaySeconds);
            for (int i = seconds; i >= 1; i--)
            {
                ShowCountdown($"Incendio tra {i}s", 1.05f);
                yield return new WaitForSeconds(1f);
            }
        }
        else
        {
            yield return new WaitForSeconds(Mathf.Max(0f, fireStartDelaySeconds));
        }

        ActivateRandomFire();


        fireActive = true;
        state = ScenarioState.Incendio;

        sessionStartTime = Time.time;
        sessionRunning = true;
        sessionEndTime = -1f;

        ConsoleLogger.SetTimeProvider(() => SessionElapsed);



        // Guida solo se feedback ON
        string classe = selectedFire.fireClass.ToString(); // Converte l'enum in stringa
        ShowGuidance($"Incendio di classe {classe} attivo. Attiva l'allarme.", 3.0f);

        expectedStep = ExpectedStep.AttivaAllarme;
    }

    private IEnumerator ResetRoutine()
    {
        if (disableDuringReset != null)
        {
            foreach (var go in disableDuringReset)
                if (go != null) go.SetActive(false);
        }

        yield return null;
        yield return new WaitForEndOfFrame();

        ResetPlayerToSpawn();
        yield return null;

        yield return StartCoroutine(ResetExtinguishersPoseRoutine());

        if (disableDuringReset != null)
        {
            foreach (var go in disableDuringReset)
                if (go != null) go.SetActive(true);
        }
    }

    public void ResetPlayerToSpawn()
    {
        if (xrOriginRoot == null || xrCamera == null || playerSpawnPoint == null)
        {
            ConsoleLogger.Warn("spawn_reset_missing_refs");
            return;
        }

        Vector3 cameraOffset = xrCamera.position - xrOriginRoot.position;
        xrOriginRoot.position = playerSpawnPoint.position - cameraOffset;

        float currentYaw = xrCamera.eulerAngles.y;
        float targetYaw = playerSpawnPoint.eulerAngles.y;
        float yawDelta = targetYaw - currentYaw;
        xrOriginRoot.RotateAround(xrCamera.position, Vector3.up, yawDelta);

        ConsoleLogger.Log("spawn_reset");
    }

    private IEnumerator ResetExtinguishersPoseRoutine()
    {
        if (extinguishers == null) yield break;

        foreach (var ext in extinguishers)
        {
            if (ext == null) continue;

            var go = ext.gameObject;
            go.SetActive(false);
            yield return null;

            if (extStartPose.TryGetValue(ext, out var pose))
                ext.transform.SetPositionAndRotation(pose.pos, pose.rot);

            go.SetActive(true);
            yield return null;

            ext.ResetExtinguisher();

            var rb = ext.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.Sleep();
            }
        }

        ConsoleLogger.Log("extinguishers_pose_reset");
    }

    // ===================== EVENTI =====================

    public void OnAlarmChanged(bool isOn)
    {
        if (isResetting || attemptEnded) return;

        if (!fireActive && isOn)
        {
            EndAsFailed("hai attivato l'allarme prima dell'inizio dell'incendio");
            return;
        }

        if (isOn)
        {
            if (expectedStep != ExpectedStep.AttivaAllarme)
            {
                EndAsFailed("hai attivato l'allarme fuori ordine");
                return;
            }

            alarmOn = true;
            state = ScenarioState.Allarme;
            expectedStep = ExpectedStep.PrendiEstintore;

            ConsoleLogger.Log("alarm_on", $"t={SessionElapsed:0.00}s");

            ExperimentFileLogger.MarkAlarm(SessionElapsed);

            //questi NON devono apparire con feedback OFF
            ShowFeedback("Allarme attivato", 2.5f);
            ShowGuidance("Prendi l'estintore.", 2.5f);
            return;
        }

        alarmOn = false;

        if (fireActive)
        {
            state = ScenarioState.Incendio;
            EndAsFailed("hai spento l'allarme mentre l'incendio era ancora attivo");
            return;
        }

        if (expectedStep != ExpectedStep.SpegniAllarme)
        {
            EndAsFailed("hai spento l'allarme fuori ordine");
            return;
        }

        expectedStep = ExpectedStep.Fine;
        EndAsCompleted();
    }

    public void OnExtinguisherGrabbed(ExtinguisherSprayer ext)
    {
        if (isResetting || attemptEnded) return;

        currentExt = ext;

        // Controlla se l'estintore viene preso prima che l'allarme sia stato attivato
        if (expectedStep == ExpectedStep.AttivaAllarme)
        {
            EndAsFailed("Hai preso l'estintore prima di attivare l'allarme");
            return;
        }

        // Se c'è un incendio attivo, verifica se l'estintore è corretto
        if (fireActive && selectedFire != null)
        {
            bool isEffective = selectedFire.recommendedExtinguishers.Contains(ext.extinguisherType) ||
                               selectedFire.secondaryExtinguishers.Contains(ext.extinguisherType);

            if (isEffective)
            {
                // Estintore corretto
                extinguisherCollected = true;
                expectedStep = ExpectedStep.SpegniIncendio;

                ConsoleLogger.Log("extinguisher_grabbed", $"name={ext.name} t={SessionElapsed:0.00}s");
                ExperimentFileLogger.MarkGrab(SessionElapsed);

                ShowFeedback("Estintore raccolto", 2.0f);
                ShowGuidance("Estintore corretto, spegni l'incendio.", 2.5f);
                return;
            }
            else
            {
                // Estintore non corretto
                ShowFeedback($"L'estintore \"{ext.extinguisherType}\" non è efficace per questo incendio.", 3.0f);
                return;
            }
        }

        // Se prendi l’estintore prima dell’inizio dell’incendio
        if (!fireActive && expectedStep != ExpectedStep.SpegniAllarme && expectedStep != ExpectedStep.Fine)
        {
            EndAsFailed("Hai preso l'estintore prima dell'inizio dell'incendio");
            return;
        }

        // Se già raccolto e fuori ordine
        if (extinguisherCollected)
        {
            ExperimentFileLogger.MarkRegrab(SessionElapsed);
            ConsoleLogger.Log("extinguisher_regrabbed", $"name={ext.name} t={SessionElapsed:0.00}s | step={expectedStep}");
            return;
        }

        // Se prendi l'estintore fuori ordine, termina la sessione
        EndAsFailed("Hai preso l'estintore in un momento non previsto");
    }

    public bool CanStartSpray(ExtinguisherSprayer ext)
    {
        if (isResetting || attemptEnded) return false;

        currentExt = ext;

        // Verifica che l'estintore sia valido prima di spruzzare
        bool isEffective = selectedFire.recommendedExtinguishers.Contains(ext.extinguisherType) ||
                           selectedFire.secondaryExtinguishers.Contains(ext.extinguisherType);

        if (!isEffective)
        {
            // Se l'estintore è errato, termina la sessione con il messaggio
            EndAsFailed("Hai utilizzato un estintore errato per questo tipo di incendio");
            return false; // Non permette lo spruzzo
        }

        if (!fireActive)
        {
            ExperimentFileLogger.MarkSprayBlocked(SessionElapsed, "fuoco_non_attivo");
            return false;
        }

        if (expectedStep != ExpectedStep.SpegniIncendio)
        {
            EndAsFailed("hai provato a spruzzare prima di attivare l'allarme e prendere l'estintore");
            return false;
        }

        return true;
    }
    private void StopAllSprays()
    {
        if (extinguishers == null) return;
        foreach (var ext in extinguishers)
            ext?.ForceStopSpray();
    }

    public void OnSprayStart()
    {
        if (!sessionRunning || attemptEnded) return;
        ConsoleLogger.Log("spray_start", $"t={SessionElapsed:0.00}s");

        ExperimentFileLogger.MarkSprayStart(SessionElapsed);
    }

    public void OnSprayStop()
    {
        if (!sessionRunning || attemptEnded) return;
        ConsoleLogger.Log("spray_stop", $"t={SessionElapsed:0.00}s");

        ExperimentFileLogger.MarkSprayStop(SessionElapsed);
    }

    public void OnFireExtinguished()
    {
        if (isResetting || attemptEnded) return;
        if (!fireActive) return;

        if (expectedStep != ExpectedStep.SpegniIncendio)
        {
            EndAsFailed("incendio spento in uno stato non previsto (ordine errato)");
            return;
        }

        fireActive = false;

        ConsoleLogger.Log("fire_extinguished", $"t={SessionElapsed:0.00}s");

        ExperimentFileLogger.MarkFireOut(SessionElapsed);

        if (alarmOn)
        {
            state = ScenarioState.Ripristino;
            expectedStep = ExpectedStep.SpegniAllarme;

            ShowFeedback("Incendio spento", 3.0f);
            ShowGuidance("Incendio spento. Spegni l'allarme.", 3.0f);
        }
        else
        {
            EndAsFailed("hai spento l'incendio senza aver attivato l'allarme");
        }
    }

    public void OnExtinguisherEmpty(ExtinguisherSprayer ext)
    {
        if (isResetting || attemptEnded) return;

        if (fireActive)
            EndAsFailed($"Estintore scarico mentre l'incendio era ancora attivo ({ext.name})");
    }

    public void RestartFromButton()
    {
        RestartSession(isInitialStart: false);
    }

    private void ActivateRandomFire()
    {
        if (fireTargets != null && fireTargets.Length > 0)
        {
            // Seleziona un fuoco casuale
            selectedFire = fireTargets[Random.Range(0, fireTargets.Length)];

            // Attiva il fuoco selezionato
            selectedFire.Ignite(); // Assicurati che Ignite() venga chiamato per attivare il fuoco
        }
    }


}