using UnityEngine;

public class AlarmSwitch : MonoBehaviour
{
    [Header("Optional visuals/audio")]
    public Light alarmLight;
    public AudioSource alarmAudio;

    [Header("State")]
    public bool isOn;

    private ScenarioFlowManager flow;

    private void Awake()
    {
        flow = FindObjectOfType<ScenarioFlowManager>();
    }

    public void Toggle()
    {
        isOn = !isOn;

        if (alarmLight != null) alarmLight.enabled = isOn;

        if (alarmAudio != null)
        {
            if (isOn) alarmAudio.Play();
            else alarmAudio.Stop();
        }

        if (isOn)
        {
            if (flow == null) flow = FindObjectOfType<ScenarioFlowManager>();
            flow?.OnAlarmTurnedOn();
        }
    }

    public void ResetAlarm()
    {
        isOn = false;
        if (alarmLight != null) alarmLight.enabled = false;
        if (alarmAudio != null) alarmAudio.Stop();
    }
}
