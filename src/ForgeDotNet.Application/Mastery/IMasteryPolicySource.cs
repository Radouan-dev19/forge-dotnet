using ForgeDotNet.Domain.Mastery;

namespace ForgeDotNet.Application.Mastery;

public interface IMasteryPolicySource
{
    MasteryPolicy Current { get; }
}

public sealed class VersionedMasteryPolicySource : IMasteryPolicySource
{
    public MasteryPolicy Current => MasteryPolicyCatalog.Version1;
}
