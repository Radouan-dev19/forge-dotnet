using ForgeDotNet.Domain.Mastery;

namespace ForgeDotNet.Application.Mastery;

public interface IMasteryEvidenceSource
{
    ValueTask<MasteryEvidenceSet> ReadAsync(Guid profileId, CancellationToken cancellationToken = default);
}
