using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;

public static class ExperimentFileLogger
{
    [Serializable]
    public class LoggedEvent
    {
        public string name;
        public float t;
        public string data;
    }

    [Serializable]
    public class AttemptRecord
    {
        public string attemptId;
        public string startUtc;
        public string endUtc;

        public bool fileLoggingOn;
        public bool feedbackOn;
        public string outcome;      // "completed" | "failed" | "aborted"
        public string failReason;

        // [NUOVI DATI]
        public string fireClass = "Nessuno";
        public string extinguisherUsed = "Nessuno";

        // Tempi (secondi dall’inizio sessione)
        public float totalTime = -1f;
        public float alarmTime = -1f;
        public float grabTime = -1f; // <-- Mantenuto!
        public float fireOutTime = -1f;

        // Spray e conteggi
        public float sprayTotalTime = 0f;
        public int regrabCount = 0; // <-- Mantenuto!

        public List<LoggedEvent> events = new List<LoggedEvent>();

        [NonSerialized] public bool _ended = false;
        [NonSerialized] public bool _spraying = false;
        [NonSerialized] public float _sprayActiveSince = -1f;
    }

    private static AttemptRecord current;
    private static string logsDir;
    private static string csvPath;
    private static bool writeFiles;

    public static string PercorsoSalvataggi => logsDir;

    private const char CSV_SEP = ';';
    private static readonly CultureInfo It = CultureInfo.GetCultureInfo("it-IT");

    // L'errore di prima è stato corretto qui: (current._ended)
    public static bool HasActiveAttempt => current != null && !current._ended;

    public static string BeginAttempt(bool feedbackOn, bool fileLoggingOn)
    {
        string id = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss_fff");
        writeFiles = fileLoggingOn;

        current = new AttemptRecord
        {
            attemptId = id,
            startUtc = DateTime.UtcNow.ToString("o"),
            feedbackOn = feedbackOn,
            fileLoggingOn = fileLoggingOn,
            outcome = "in_progress",
            failReason = ""
        };

        if (!writeFiles) return id;

        logsDir = Path.Combine(Application.persistentDataPath, "Logs");
        Directory.CreateDirectory(logsDir);

        csvPath = Path.Combine(logsDir, "attempts.csv");
        EnsureCsvHeader();

        return id;
    }

    // --- NUOVI METODI PER FUOCO ED ESTINTORE ---
    public static void SetFireClass(string fireClass)
    {
        if (current != null && !current._ended)
            current.fireClass = fireClass;
    }

    public static void SetExtinguisherUsed(string extName)
    {
        if (current != null && !current._ended)
            current.extinguisherUsed = extName;
    }
    // -------------------------------------------

    public static void LogEvent(string name, float t, object data = null)
    {
        if (current == null || current._ended) return;
        if (!writeFiles) return;

        current.events.Add(new LoggedEvent
        {
            name = name,
            t = t,
            data = data == null ? "" : data.ToString()
        });
    }

    public static void MarkAlarm(float t)
    {
        if (current == null || current._ended) return;
        if (current.alarmTime < 0f) current.alarmTime = t;
        LogEvent("alarm_on", t);
    }

    // --- METODI MANTENUTI ---
    public static void MarkGrab(float t)
    {
        if (current == null || current._ended) return;
        if (current.grabTime < 0f) current.grabTime = t;
        LogEvent("extinguisher_grabbed", t);
    }

    public static void MarkRegrab(float t)
    {
        if (current == null || current._ended) return;
        current.regrabCount++;
        LogEvent("extinguisher_regrabbed", t, $"count={current.regrabCount}");
    }
    // ------------------------

    public static void MarkSprayStart(float t)
    {
        if (current == null || current._ended) return;
        if (!current._spraying)
        {
            current._spraying = true;
            current._sprayActiveSince = t;
        }
        LogEvent("spray_start", t);
    }

    public static void MarkSprayStop(float t)
    {
        if (current == null || current._ended) return;

        if (current._spraying)
        {
            current._spraying = false;
            float dt = Mathf.Max(0f, t - current._sprayActiveSince);
            current.sprayTotalTime += dt;
            current._sprayActiveSince = -1f;
        }

        LogEvent("spray_stop", t, $"sprayTotal={current.sprayTotalTime.ToString("0.00", It)}");
    }

    public static void MarkFireOut(float t)
    {
        if (current == null || current._ended) return;
        if (current.fireOutTime < 0f) current.fireOutTime = t;
        LogEvent("fire_extinguished", t);
    }

    public static void EndAttempt(string outcome, float totalTime, string failReason = "")
    {
        if (current == null || current._ended) return;

        if (current._spraying)
        {
            current._spraying = false;
            float dt = Mathf.Max(0f, totalTime - current._sprayActiveSince);
            current.sprayTotalTime += dt;
            current._sprayActiveSince = -1f;
        }

        current._ended = true;
        current.outcome = outcome;
        current.totalTime = totalTime;
        current.failReason = failReason ?? "";
        current.endUtc = DateTime.UtcNow.ToString("o");

        LogEvent("attempt_end", totalTime, $"outcome={outcome}");

        if (!writeFiles)
        {
            current = null;
            return;
        }

        string jsonPath = Path.Combine(logsDir, $"attempt_{current.attemptId}.json");
        File.WriteAllText(jsonPath, JsonUtility.ToJson(current, true));

        AppendCsvLine(current);
        current = null;
    }

    private static void EnsureCsvHeader()
    {
        if (File.Exists(csvPath)) return;

        string header = string.Join(CSV_SEP.ToString(), new[]
        {
        "attemptId","startUtc","endUtc","feedbackOn","outcome","totalTime","failReason",
        "fireClass","extinguisherUsed","alarmTime","grabTime","sprayTotalTime","fireOutTime","regrabCount"
    });

        File.WriteAllText(csvPath, header + Environment.NewLine);
    }

    private static void AppendCsvLine(AttemptRecord r)
    {
        string line = string.Join(CSV_SEP.ToString(), new[]
        {
        Csv(r.attemptId), Csv(r.startUtc), Csv(r.endUtc), Csv(r.feedbackOn ? "1" : "0"),
        Csv(r.outcome), Csv(F(r.totalTime)), Csv(r.failReason),
        Csv(r.fireClass), Csv(r.extinguisherUsed),
        Csv(F(r.alarmTime)), Csv(F(r.grabTime)), Csv(F(r.sprayTotalTime)),
        Csv(F(r.fireOutTime)), Csv(r.regrabCount.ToString())
    });

        try
        {
            File.AppendAllText(csvPath, line + Environment.NewLine);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[ATTENZIONE] File CSV bloccato da Excel! Errore: {e.Message}");
        }
    }

    private static string F(float v)
    {
        return v < 0f ? "" : v.ToString("0.00", It);
    }

    private static string Csv(string s)
    {
        if (s == null) s = "";
        s = s.Replace("\"", "\"\"");
        return $"\"{s}\"";
    }
}