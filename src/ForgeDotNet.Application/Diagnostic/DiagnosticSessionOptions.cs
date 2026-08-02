namespace ForgeDotNet.Application.Diagnostic;

public sealed class DiagnosticSessionOptions
{
    public TimeSpan InitialSectionDuration { get; init; } = TimeSpan.FromMinutes(30);

    public TimeSpan ReducedSectionDuration { get; init; } = TimeSpan.FromMinutes(2);

    public TimeSpan GetSectionDuration(Domain.Diagnostic.DiagnosticMode mode)
    {
        TimeSpan duration = mode == Domain.Diagnostic.DiagnosticMode.Initial
            ? InitialSectionDuration
            : ReducedSectionDuration;
        if (duration < TimeSpan.FromSeconds(1) || duration > TimeSpan.FromHours(2))
        {
            throw new InvalidOperationException("La durée d'une section de diagnostic est invalide.");
        }

        return duration;
    }
}
