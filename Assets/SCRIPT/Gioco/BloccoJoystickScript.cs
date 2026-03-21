using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

[RequireComponent(typeof(XRGrabInteractable))]
public class BloccaJoystickPresa : MonoBehaviour
{
    private XRGrabInteractable grabInteractable;
    private IXRSelectInteractor interactorAttuale;

    // Variabili per memorizzare la posizione e rotazione bloccate
    private Vector3 posizioneAncoraBloccata;
    private Quaternion rotazioneAncoraBloccata;

    private void Awake()
    {
        grabInteractable = GetComponent<XRGrabInteractable>();
        grabInteractable.selectEntered.AddListener(OnGrab);
        grabInteractable.selectExited.AddListener(OnRelease);
    }

    private void OnDestroy()
    {
        if (grabInteractable != null)
        {
            grabInteractable.selectEntered.RemoveListener(OnGrab);
            grabInteractable.selectExited.RemoveListener(OnRelease);
        }
    }

    private void OnGrab(SelectEnterEventArgs args)
    {
        interactorAttuale = args.interactorObject;

        // Salviamo la distanza esatta e la rotazione nel momento in cui afferriamo l'oggetto
        Transform attachTransform = interactorAttuale.GetAttachTransform(grabInteractable);
        if (attachTransform != null)
        {
            posizioneAncoraBloccata = attachTransform.localPosition;
            rotazioneAncoraBloccata = attachTransform.localRotation;
        }
    }

    private void OnRelease(SelectExitEventArgs args)
    {
        interactorAttuale = null;
    }

    // Usiamo LateUpdate così applichiamo il blocco DOPO che il joystick ha provato a muoverlo
    private void LateUpdate()
    {
        if (interactorAttuale != null)
        {
            Transform attachTransform = interactorAttuale.GetAttachTransform(grabInteractable);
            if (attachTransform != null)
            {
                // Sovrascriviamo qualsiasi movimento del joystick forzando i valori salvati
                attachTransform.localPosition = posizioneAncoraBloccata;
                attachTransform.localRotation = rotazioneAncoraBloccata;
            }
        }
    }
}