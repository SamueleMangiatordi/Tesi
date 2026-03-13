using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class PauseManager : MonoBehaviour
{
    public static bool IsPaused { get; private set; }

    // [AGGIUNTO] La nostra "serratura". Se è falso, i tasti della pausa vengono ignorati.
    public bool canPause = true;

    [Header("UI")]
    [SerializeField] private GameObject pausePanel;

    [Header("Input")]
    [SerializeField] private KeyCode pauseKey = KeyCode.Space;

    [Header("Disabilita durante la pausa (Locomotion)")]
    [SerializeField] private GameObject[] disableDuringPause;

    [Header("XR Interactors (Mani e Laser)")]
    [Tooltip("Trascina qui i Left e Right Controller che contengono i Ray/Direct Interactors")]
    [SerializeField] private XRBaseInteractor[] xrInteractors;

    private float defaultFixedDelta;
    private int[] defaultInteractionLayers;

    private void Awake()
    {
        defaultFixedDelta = Time.fixedDeltaTime;
        if (pausePanel != null) pausePanel.SetActive(false);
        IsPaused = false;

        if (xrInteractors != null && xrInteractors.Length > 0)
        {
            defaultInteractionLayers = new int[xrInteractors.Length];
            for (int i = 0; i < xrInteractors.Length; i++)
            {
                if (xrInteractors[i] != null)
                    defaultInteractionLayers[i] = xrInteractors[i].interactionLayers.value;
            }
        }
    }

    private void Update()
    {
        // [AGGIUNTO] Se la serratura è chiusa (es. fine partita), ignora qualsiasi tasto premuto!
        if (!canPause) return;

#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
            TogglePause();
#else
        if (Input.GetKeyDown(pauseKey))
            TogglePause();
#endif
    }

    public void OnPauseButtonClicked()
    {
        // [AGGIUNTO] Blocca anche il click fisico sul bottone se la pausa non è permessa
        if (!canPause) return;

        TogglePause();
    }

    public void TogglePause()
    {
        if (IsPaused) Resume();
        else Pause();
    }

    public void Pause()
    {
        if (IsPaused) return;
        IsPaused = true;

        if (pausePanel != null) pausePanel.SetActive(true);

        Time.timeScale = 0f;
        Time.fixedDeltaTime = 0f;
        AudioListener.pause = true;

        if (disableDuringPause != null)
            foreach (var go in disableDuringPause)
                if (go != null) go.SetActive(false);

        // Disattiva il grab degli oggetti fisici mantenendo i laser
        if (xrInteractors != null)
        {
            for (int i = 0; i < xrInteractors.Length; i++)
            {
                if (xrInteractors[i] != null)
                    xrInteractors[i].interactionLayers = 0;
            }
        }
    }

    public void Resume()
    {
        if (!IsPaused) return;
        IsPaused = false;

        if (pausePanel != null) pausePanel.SetActive(false);

        Time.timeScale = 1f;
        Time.fixedDeltaTime = defaultFixedDelta;
        AudioListener.pause = false;

        if (disableDuringPause != null)
            foreach (var go in disableDuringPause)
                if (go != null) go.SetActive(true);

        // Ripristina il grab degli oggetti fisici
        if (xrInteractors != null && defaultInteractionLayers != null)
        {
            for (int i = 0; i < xrInteractors.Length; i++)
            {
                if (xrInteractors[i] != null)
                    xrInteractors[i].interactionLayers = defaultInteractionLayers[i];
            }
        }
    }

    public void GoToMenu(string menuSceneName)
    {
        Resume();

        // Scongela il tempo in modo assoluto. Fondamentale se esci da una partita finita
        Time.timeScale = 1f;

        SceneManager.LoadScene(menuSceneName);
    }
}