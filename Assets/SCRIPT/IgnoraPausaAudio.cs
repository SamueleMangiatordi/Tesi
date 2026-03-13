using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class IgnoraPausaAudio : MonoBehaviour
{
    private void Awake()
    {
        // Dice all'Audio Source di ignorare la sordità globale causata dalla pausa
        GetComponent<AudioSource>().ignoreListenerPause = true;
    }
}