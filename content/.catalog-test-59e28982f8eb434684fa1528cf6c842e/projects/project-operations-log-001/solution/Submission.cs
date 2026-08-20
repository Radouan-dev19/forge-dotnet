using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;

public static class Submission
{
    public static string JournalRun(string steps, string minimumLevel)
    {
        var log = new MemoryLog(ParseLevel(minimumLevel));
        foreach (string step in steps.Split(';'))
        {
            string[] parts = step.Split(':');
            string name = parts[0];
            string status = parts[1];

            // Le niveau vient de la nature de l'étape ; le puits décide seul de ce qu'il retient.
            if (status == "ok")
            {
                log.LogInformation("{Step} terminé", name);
            }
            else if (status == "reprise")
            {
                log.LogWarning("{Step} rejoué", name);
            }
            else
            {
                log.LogError("{Step} en échec : {Detail}", name, parts[2]);
            }
        }

        return Render(log);
    }

    public static string SecureEntry(string message, string secrets)
    {
        string redacted = message;
        foreach (string secret in secrets.Split(';'))
        {
            if (secret.Length > 0)
            {
                redacted = redacted.Replace(secret, "***");
            }
        }

        // Le caviardage précède l'émission : un secret arrivé au puits est déjà une fuite.
        var log = new MemoryLog(LogLevel.Information);
        log.LogInformation("{Message}", redacted);
        return Render(log);
    }

    public static string CorrelatedJournal(string requestId, string steps)
    {
        var log = new MemoryLog(LogLevel.Information);
        using (log.BeginScope($"cid={requestId}"))
        {
            foreach (string step in steps.Split(';'))
            {
                int separator = step.IndexOf('/');
                if (separator < 0)
                {
                    log.LogInformation("{Step}", step);
                    continue;
                }

                // La portée imbriquée vit exactement le temps de la sous-étape : la refermer
                // avant de continuer est ce qui empêche sa contamination des étapes suivantes.
                using (log.BeginScope(step[..separator]))
                {
                    log.LogInformation("{Action}", step[(separator + 1)..]);
                }
            }
        }

        return Render(log);
    }

    // ======================= FOURNI — ne pas modifier =======================

    /// <summary>Puits de journal : seuil, portées empilées et entrées capturées.</summary>
    public sealed class MemoryLog(LogLevel minimum) : ILogger
    {
        private readonly List<string> _entries = [];
        private readonly Stack<string> _scopes = new();

        public IReadOnlyList<string> Entries => _entries;

        public bool IsEnabled(LogLevel logLevel) => logLevel >= minimum;

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull
        {
            _scopes.Push(state.ToString() ?? string.Empty);
            return new ScopePopper(_scopes);
        }

        public void Log<TState>(
            LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel))
            {
                return;
            }

            string prefix = _scopes.Count == 0 ? string.Empty : string.Join(">", _scopes.Reverse()) + " ";
            _entries.Add($"{Abbreviation(logLevel)} {prefix}{formatter(state, exception)}");
        }

        private static string Abbreviation(LogLevel level) => level switch
        {
            LogLevel.Debug => "DBG",
            LogLevel.Information => "INF",
            LogLevel.Warning => "WRN",
            LogLevel.Error => "ERR",
            _ => "AUT",
        };

        private sealed class ScopePopper(Stack<string> scopes) : IDisposable
        {
            public void Dispose() => scopes.Pop();
        }
    }

    public static LogLevel ParseLevel(string name) => name switch
    {
        "debug" => LogLevel.Debug,
        "information" => LogLevel.Information,
        "warning" => LogLevel.Warning,
        "error" => LogLevel.Error,
        _ => throw new ArgumentOutOfRangeException(nameof(name)),
    };

    public static string Render(MemoryLog log) =>
        log.Entries.Count == 0 ? "(vide)" : string.Join("|", log.Entries);
}
