using ForgeDotNet.Domain.SqlLab;

namespace ForgeDotNet.Application.SqlLab;

/// <summary>
/// Source des scénarios SQL publiés, confinée au serveur.
/// </summary>
public interface ISqlScenarioSource
{
    ValueTask<IReadOnlyList<SqlScenario>> ListAsync(CancellationToken cancellationToken = default);

    ValueTask<SqlScenario?> GetAsync(string scenarioId, CancellationToken cancellationToken = default);
}
