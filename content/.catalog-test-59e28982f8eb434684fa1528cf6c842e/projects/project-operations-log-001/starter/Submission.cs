using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;

public static class Submission
{
    // ---------------------------------------------------------------------------------------
    // À VOUS : les trois méthodes ci-dessous. Le puits MemoryLog, ParseLevel et Render sont
    // fournis plus bas — les modifier ferait échouer vos propres cas.
    // ---------------------------------------------------------------------------------------

    /// <summary>Émet chaque étape au niveau imposé par son statut, puis rend le journal capturé.</summary>
    public static string JournalRun(string steps, string minimumLevel)
    {
        throw new NotImplementedException();
    }

    /// <summary>Caviarde chaque occurrence de chaque secret, émet en Information, rend l'entrée.</summary>
    public static string SecureEntry(string message, string secrets)
    {
        throw new NotImplementedException();
    }

    /// <summary>Corrèle les étapes par la portée « cid=… », jamais par le texte des messages.</summary>
    public static string CorrelatedJournal(string requestId, string steps)
    {
        throw new NotImplementedException();
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
