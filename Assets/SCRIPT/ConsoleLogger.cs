using System;
using UnityEngine;

public static class ConsoleLogger
{
    private static Func<float> timeProvider;

    // Se impostato, useremo questo tempo (es. tempo dall’inizio sessione)
    public static void SetTimeProvider(Func<float> provider)
    {
        timeProvider = provider;
    }

    private static float Now => timeProvider != null ? timeProvider() : Time.time;

    public static void Log(string eventName, object data = null)
    {
        string payload = data == null ? "" : $" | data: {data}";
        Debug.Log($"[LOG] t={Now:0.00}s | {eventName}{payload}");
    }

    public static void Warn(string eventName, object data = null)
    {
        string payload = data == null ? "" : $" | data: {data}";
        Debug.LogWarning($"[LOG] t={Now:0.00}s | {eventName}{payload}");
    }
}
