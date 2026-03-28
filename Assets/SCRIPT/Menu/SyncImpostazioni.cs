using UnityEngine;
using UnityEngine.UI; // Fondamentale per usare la classe Toggle

public class SyncImpostazioniUI : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private Toggle aiutiToggle;

    private void Start()
    {
        // Quando si carica il menu, controlliamo se il Singleton esiste già
        if (ExperimentSettings.Instance != null)
        {
            // Impostiamo la spunta del Toggle basandoci sul valore salvato nel Singleton.
            // Usiamo "SetIsOnWithoutNotify" per evitare che Unity creda che il giocatore 
            // abbia cliccato il Toggle in questo esatto momento.
            aiutiToggle.SetIsOnWithoutNotify(ExperimentSettings.FeedbackOn);
        }
    }
}
