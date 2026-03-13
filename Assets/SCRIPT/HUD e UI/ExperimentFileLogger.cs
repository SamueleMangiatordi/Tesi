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

        // Tempi (secondi dall’inizio sessione)
        public float totalTime = -1f;
        public float alarmTime = -1f;
        public float grabTime = -1f;
        public float pinTime = -1f;
        public float fireOutTime = -1f;

        // Spray
        public int sprayStartCount = 0;
        public int sprayBlockedCount = 0;
        public float sprayTotalTime = 0f;

        // Conteggi
        public int regrabCount = 0;

        // Eventi (opzionale ma utile)
        public List<LoggedEvent> events = new List<LoggedEvent>();

        // runtime (non serializzare idealmente, ma JsonUtility li include: li teniamo privati)
        [NonSerialized] public bool _ended = false;
        [NonSerialized] public bool _spraying = false;
        [NonSerialized] public float _sprayActiveSince = -1f;
    }

    private static AttemptRecord current;
    private static string logsDir;
    private static string csvPath;
    private static bool writeFiles;

    public static string PercorsoSalvataggi => logsDir;


    // In Italia Excel usa spesso ';' come separatore
    private const char CSV_SEP = ';';
    private static readonly CultureInfo It = CultureInfo.GetCultureInfo("it-IT");

    public static bool HasActiveAttempt => current != null && !current._ended;

    public static string BeginAttempt(bool feedbackOn, bool fileLoggingOn)
    {
        string id = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss_fff");

        // lock della scelta: vale per tutto il tentativo
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

        // Se file logging è OFF, NON preparo cartelle/file.
        if (!writeFiles)
        {
            // Volendo puoi tenere comunque gli eventi in RAM (come ora)
            LogEvent("attempt_begin", 0f, $"feedbackOn={feedbackOn} | fileLoggingOn={fileLoggingOn}");
            return id;
        }

        logsDir = Path.Combine(Application.persistentDataPath, "Logs");
        Directory.CreateDirectory(logsDir);

        csvPath = Path.Combine(logsDir, "attempts.csv");
        EnsureCsvHeader();

        LogEvent("attempt_begin", 0f, $"feedbackOn={feedbackOn} | fileLoggingOn={fileLoggingOn}");
        return id;
    }


    public static void LogEvent(string name, float t, object data = null)
    {
        if (current == null || current._ended) return;
        if (!writeFiles) return; // opzionale

        current.events.Add(new LoggedEvent
        {
            name = name,
            t = t,
            data = data == null ? "" : data.ToString()
        });
    }


    public static void MarkAlarm(float t)
    {
        if (!HasActiveAttempt) return;
        if (current.alarmTime < 0f) current.alarmTime = t;
        LogEvent("alarm_on", t);
    }

    public static void MarkGrab(float t)
    {
        if (!HasActiveAttempt) return;
        if (current.grabTime < 0f) current.grabTime = t;
        LogEvent("extinguisher_grabbed", t);
    }

    public static void MarkRegrab(float t)
    {
        if (!HasActiveAttempt) return;
        current.regrabCount++;
        LogEvent("extinguisher_regrabbed", t, $"count={current.regrabCount}");
    }

    public static void MarkPin(float t)
    {
        if (!HasActiveAttempt) return;
        if (current.pinTime < 0f) current.pinTime = t;
        LogEvent("safety_pin_removed", t);
    }

    public static void MarkSprayStart(float t)
    {
        if (!HasActiveAttempt) return;
        current.sprayStartCount++;
        if (!current._spraying)
        {
            current._spraying = true;
            current._sprayActiveSince = t;
        }
        LogEvent("spray_start", t, $"starts={current.sprayStartCount}");
    }

    public static void MarkSprayStop(float t)
    {
        if (!HasActiveAttempt) return;

        if (current._spraying)
        {
            current._spraying = false;
            float dt = Mathf.Max(0f, t - current._sprayActiveSince);
            current.sprayTotalTime += dt;
            current._sprayActiveSince = -1f;
        }

        LogEvent("spray_stop", t, $"sprayTotal={current.sprayTotalTime.ToString("0.00", It)}");
    }

    public static void MarkSprayBlocked(float t, string reason)
    {
        if (!HasActiveAttempt) return;
        current.sprayBlockedCount++;
        LogEvent("spray_blocked", t, reason);
    }

    public static void MarkFireOut(float t)
    {
        if (!HasActiveAttempt) return;
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

        // BLOCCO QUI
        if (!writeFiles)
        {
            current = null;
            return;
        }

        string jsonPath = Path.Combine(logsDir, $"attempt_{current.attemptId}.json");
        File.WriteAllText(jsonPath, JsonUtility.ToJson(current, true));

        AppendCsvLine(current);

        Debug.Log($"[ExperimentFileLogger] Salvati log in: {logsDir}");
        current = null;
    }


    private static void EnsureCsvHeader()
    {
        if (File.Exists(csvPath)) return;

        string header =
            string.Join(CSV_SEP.ToString(), new[]
            {
                "attemptId","startUtc","endUtc","feedbackOn","outcome","totalTime","failReason",
                "alarmTime","grabTime","pinTime","sprayStartCount","sprayBlockedCount","sprayTotalTime","fireOutTime","regrabCount"
            });

        File.WriteAllText(csvPath, header + Environment.NewLine);
    }

    private static void AppendCsvLine(AttemptRecord r)
    {
        string line = string.Join(CSV_SEP.ToString(), new[]
        {
            Csv(r.attemptId),
            Csv(r.startUtc),
            Csv(r.endUtc),
            Csv(r.feedbackOn ? "1" : "0"),
            Csv(r.outcome),
            Csv(F(r.totalTime)),
            Csv(r.failReason),

            Csv(F(r.alarmTime)),
            Csv(F(r.grabTime)),
            Csv(F(r.pinTime)),

            Csv(r.sprayStartCount.ToString()),
            Csv(r.sprayBlockedCount.ToString()),
            Csv(F(r.sprayTotalTime)),

            Csv(F(r.fireOutTime)),
            Csv(r.regrabCount.ToString())
        });

        File.AppendAllText(csvPath, line + Environment.NewLine);
    }

    private static string F(float v)
    {
        return v < 0f ? "" : v.ToString("0.00", It);
    }

    private static string Csv(string s)
    {
        if (s == null) s = "";
        // escape basilare per CSV: " -> ""
        s = s.Replace("\"", "\"\"");
        return $"\"{s}\"";
    }
}
