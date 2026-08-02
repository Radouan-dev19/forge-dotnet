using ForgeDotNet.Domain.Diagnostic;

namespace ForgeDotNet.Application.Diagnostic;

public interface IDiagnosticRubricSource
{
    ValueTask<DiagnosticScoringRubric> GetRubricAsync(CancellationToken cancellationToken = default);
}
