using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class VideoLoading : MonoBehaviour
{
    public string targetScene = "GameplayScene";
    public float minVideoTime = 1.0f; // almeno 1s di video prima di entrare

    IEnumerator Start()
    {
        // avvia caricamento in background
        AsyncOperation op = SceneManager.LoadSceneAsync(targetScene);
        op.allowSceneActivation = false;

        // assicura che il video resti un minimo
        yield return new WaitForSeconds(minVideoTime);

        // aspetta che il caricamento arrivi al 90%
        while (op.progress < 0.9f)
            yield return null;

        // entra nella scena
        op.allowSceneActivation = true;
    }
}