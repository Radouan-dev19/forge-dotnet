using ForgeDotNet.Domain.DebugLab;

namespace ForgeDotNet.Application.DebugLab;

public interface IDebugScenarioSource
{
    ValueTask<IReadOnlyList<DebugScenario>> ListAsync(CancellationToken cancellationToken = default);

    ValueTask<DebugScenario?> GetAsync(string scenarioId, CancellationToken cancellationToken = default);
}
