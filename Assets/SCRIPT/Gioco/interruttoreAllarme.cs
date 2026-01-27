using UnityEngine;

public class AlarmSwitch : MonoBehaviour
{
    [Header("Optional visuals/audio")]
    public Light alarmLight;
    public AudioSource alarmAudio;

    [Header("Flow (optional, auto-find)")]
    public ScenarioFlowManager flow;

    [Header("State")]
    public bool isOn;

    private void Awake()
    {
        if (flow == null) flow = FindObjectOfType<ScenarioFlowManager>();
    }

    public void Toggle()
    {
        SetOn(!isOn);
    }

    public void SetOn(bool on)
    {
        isOn = on;

        if (alarmLight != null) alarmLight.enabled = isOn;

        if (alarmAudio != null)
        {
            if (isOn)
            {
                if (!alarmAudio.isPlaying) alarmAudio.Play();
            }
            else
            {
                if (alarmAudio.isPlaying) alarmAudio.Stop();
            }
        }

        if (flow == null) flow = FindObjectOfType<ScenarioFlowManager>();
        flow?.OnAlarmChanged(isOn);
    }
}
