using System.Collections;
using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class ScenarioFlowManager : MonoBehaviour
{
    public enum ScenarioState { Idle, Alarmed, Response, Completed, Failed }

    [Header("References (Scenario)")]
    public AlarmSwitch alarmSwitch;
    public FireTarget fireTarget;
    public ExtinguisherSprayer extinguisherSprayer;

    [Header("Player Reset")]
    public Transform xrOriginRoot;
    public Transform xrCamera;
    public Transform playerSpawnPoint;
    public GameObject[] disableDuringReset;

    [Header("Object Reset")]
    [Tooltip("Se assegnato, usa questo come spawn estintore; altrimenti usa la posa iniziale")]
    public Transform extinguisherSpawnPoint;

    [Header("Options")]
    public bool igniteFireOnRestart = true;

    [Header("Restart (Keyboard)")]
    public bool allowKeyboardRestart = true;
    public KeyCode restartKey = KeyCode.R;

    [Header("Runtime")]
    public ScenarioState state = ScenarioState.Idle;

    // Timer sessione
    private float sessionStartTime;
    public float SessionElapsed => Time.time - sessionStartTime;

    // Metriche minime
    private float alarmTime = -1f;
    private float grabTime = -1f;
    private float completedTime = -1f;

    // Contatori
    private int sprayStartCount = 0;
    private int sprayStopCount = 0;

    // Pose iniziale estintore
    private Vector3 extinguisherStartPos;
    private Quaternion extinguisherStartRot;

    private Coroutine resetRoutine;

    public float TimeToAlarm => (alarmTime < 0f) ? -1f : (alarmTime - sessionStartTime);
    public float TimeToGrab => (grabTime < 0f) ? -1f : (grabTime - sessionStartTime);
    public float TimeToComplete => (completedTime < 0f) ? -1f : (completedTime - sessionStartTime);

    private void Start()
    {
        CacheExtinguisherStartPose();

        // ✅ Avvio iniziale: messaggio “Sessione avviata”
        RestartSession(isInitialStart: true);
    }

    private void CacheExtinguisherStartPose()
    {
        if (extinguisherSprayer == null) return;

        if (extinguisherSpawnPoint != null)
        {
            extinguisherStartPos = extinguisherSpawnPoint.position;
            extinguisherStartRot = extinguisherSpawnPoint.rotation;
        }
        else
        {
            var t = extinguisherSprayer.transform;
            extinguisherStartPos = t.position;
            extinguisherStartRot = t.rotation;
        }
    }

    private void Update()
    {
        if (!allowKeyboardRestart) return;

#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current == null) return;

        bool pressed = restartKey switch
        {
            KeyCode.R => Keyboard.current.rKey.wasPressedThisFrame,
            KeyCode.Space => Keyboard.current.spaceKey.wasPressedThisFrame,
            KeyCode.Return => Keyboard.current.enterKey.wasPressedThisFrame,
            KeyCode.Escape => Keyboard.current.escapeKey.wasPressedThisFrame,
            _ => false
        };

        if (pressed) RestartSession(isInitialStart: false);
#endif
    }

    private void ShowFeedback(string msg, float seconds = 3f)
    {
        if (ExperimentSettings.FeedbackOn)
            FeedbackUI.Instance?.ShowTemp(msg, seconds);
    }

    public void RestartSession(bool isInitialStart)
    {
        // reset timer sessione
        sessionStartTime = Time.time;
        ConsoleLogger.SetTimeProvider(() => SessionElapsed);

        // reset metriche/contatori
        alarmTime = -1f;
        grabTime = -1f;
        completedTime = -1f;
        sprayStartCount = 0;
        sprayStopCount = 0;

        // reset stato
        state = ScenarioState.Idle;

        // reset scenario
        if (alarmSwitch != null) alarmSwitch.ResetAlarm();

        if (fireTarget != null)
        {
            if (igniteFireOnRestart) fireTarget.Ignite();
            else fireTarget.ExtinguishImmediateForIdle();
        }

        // reset player + oggetti
        if (resetRoutine != null) StopCoroutine(resetRoutine);
        resetRoutine = StartCoroutine(ResetRoutine());

        // log + feedback differenziati
        if (isInitialStart)
        {
            ConsoleLogger.Log("start_session");
            ShowFeedback("Sessione avviata", 2.5f);
        }
        else
        {
            ConsoleLogger.Log("restart_session");
            ShowFeedback("Sessione riavviata", 2f);
        }
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

        yield return StartCoroutine(ResetExtinguisherPoseRoutine());

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

    private IEnumerator ResetExtinguisherPoseRoutine()
    {
        if (extinguisherSprayer == null) yield break;

        var extGo = extinguisherSprayer.gameObject;

        // forzo drop/refresh
        extGo.SetActive(false);
        yield return null;

        extinguisherSprayer.transform.SetPositionAndRotation(extinguisherStartPos, extinguisherStartRot);

        extGo.SetActive(true);
        yield return null;

        extinguisherSprayer.ResetExtinguisher();

        var rb = extinguisherSprayer.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.Sleep();
        }

        ConsoleLogger.Log("extinguisher_pose_reset");
    }

    // ======= EVENTI =======

    public void OnAlarmTurnedOn()
    {
        if (state == ScenarioState.Completed || state == ScenarioState.Failed) return;

        if (alarmTime < 0f) alarmTime = Time.time;
        if (state == ScenarioState.Idle) state = ScenarioState.Alarmed;

        ConsoleLogger.Log("alarm_on", $"tAlarm={TimeToAlarm:0.00}s");
        ShowFeedback("Allarme attivato!", 2.5f);
    }

    public void OnExtinguisherGrabbed()
    {
        if (state == ScenarioState.Completed || state == ScenarioState.Failed) return;

        if (grabTime < 0f) grabTime = Time.time;
        if (state == ScenarioState.Alarmed || state == ScenarioState.Idle) state = ScenarioState.Response;

        ConsoleLogger.Log("extinguisher_grabbed", $"tGrab={TimeToGrab:0.00}s");
        ShowFeedback("Estintore raccolto", 2f);
    }

    public void OnSprayStart()
    {
        sprayStartCount++;
        ConsoleLogger.Log("spray_start", $"count={sprayStartCount}");
    }

    public void OnSprayStop()
    {
        sprayStopCount++;
        ConsoleLogger.Log("spray_stop", $"count={sprayStopCount}");
    }

    public void OnFireExtinguished()
    {
        if (completedTime < 0f) completedTime = Time.time;
        state = ScenarioState.Completed;

        ConsoleLogger.Log("fire_extinguished",
            $"total={TimeToComplete:0.00}s | alarm={TimeToAlarm:0.00}s | grab={TimeToGrab:0.00}s | sprayStart={sprayStartCount}");

        ShowFeedback("Fuoco spento!", 5f);
    }
}
