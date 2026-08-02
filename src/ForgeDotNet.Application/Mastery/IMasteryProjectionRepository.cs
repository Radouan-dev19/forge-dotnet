using ForgeDotNet.Domain.Mastery;

namespace ForgeDotNet.Application.Mastery;

public interface IMasteryProjectionRepository
{
    ValueTask<MasterySnapshot?> GetAsync(
        Guid profileId,
        string policyRevision,
        string evidenceRevision,
        CancellationToken cancellationToken = default);

    ValueTask<MasterySnapshot> AppendAsync(
        MasteryPolicy policy,
        MasterySnapshot snapshot,
        CancellationToken cancellationToken = default);
}
