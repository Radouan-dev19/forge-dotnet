using ForgeDotNet.Domain.Diagnostic;

namespace ForgeDotNet.Application.Diagnostic;

public interface IDiagnosticBankSource
{
    ValueTask<DiagnosticBank> GetAsync(CancellationToken cancellationToken = default);
}
