using ForgeDotNet.Domain.IdentityLocal;

namespace ForgeDotNet.UnitTests;

public sealed class UserProfileTests
{
    private static readonly DateTimeOffset CreatedAtUtc = new(2026, 7, 25, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void DefaultProfileHasHonestInitialValues()
    {
        var profile = UserProfile.CreateDefault(CreatedAtUtc);

        Assert.NotEqual(Guid.Empty, profile.LocalId);
        Assert.Equal(string.Empty, profile.DisplayName);
        Assert.Equal(string.Empty, profile.ProfessionalGoal);
        Assert.Equal(UserProfile.DefaultWeeklyHours, profile.WeeklyAvailableHours);
        Assert.Equal(InterfaceLanguage.French, profile.InterfaceLanguage);
        Assert.Equal(CreatedAtUtc, profile.CreatedAtUtc);
        Assert.False(profile.HasAcceptedLearningContract);
    }

    [Fact]
    public void UpdateCreatesAValidatedProfileWithoutChangingIdentity()
    {
        var initial = UserProfile.CreateDefault(CreatedAtUtc);

        var updated = initial.Update("  Radouan  ", "  Développeur backend .NET  ", 12, InterfaceLanguage.French);

        Assert.Equal(initial.LocalId, updated.LocalId);
        Assert.Equal(initial.CreatedAtUtc, updated.CreatedAtUtc);
        Assert.Equal("Radouan", updated.DisplayName);
        Assert.Equal("Développeur backend .NET", updated.ProfessionalGoal);
        Assert.Equal(12, updated.WeeklyAvailableHours);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(41)]
    public void UpdateRejectsInvalidWeeklyHours(int weeklyHours)
    {
        var profile = UserProfile.CreateDefault(CreatedAtUtc);

        var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
            profile.Update("Radouan", "Backend .NET", weeklyHours, InterfaceLanguage.French));

        Assert.Equal("weeklyAvailableHours", exception.ParamName);
    }

    [Fact]
    public void LearningContractControlsPathActivationWithoutBlockingProfile()
    {
        var profile = UserProfile.CreateDefault(CreatedAtUtc);

        Assert.False(LearningContract.IsLearningPathActivated(profile));

        var accepted = profile.SetLearningContractAcceptance(accepted: true);
        Assert.True(LearningContract.IsLearningPathActivated(accepted));

        var declinedAgain = accepted.SetLearningContractAcceptance(accepted: false);
        Assert.False(LearningContract.IsLearningPathActivated(declinedAgain));
    }

    [Fact]
    public void RestoreRehydratesAValidatedPersistedProfile()
    {
        var localId = Guid.NewGuid();

        var profile = UserProfile.Restore(
            localId,
            "Radouan",
            "Backend .NET",
            12,
            InterfaceLanguage.French,
            CreatedAtUtc,
            hasAcceptedLearningContract: true);

        Assert.Equal(localId, profile.LocalId);
        Assert.Equal("Radouan", profile.DisplayName);
        Assert.True(profile.HasAcceptedLearningContract);
    }

    [Fact]
    public void RestoreRejectsInvalidPersistedIdentity()
    {
        var exception = Assert.Throws<ArgumentException>(() => UserProfile.Restore(
            Guid.Empty,
            "Radouan",
            "Backend .NET",
            12,
            InterfaceLanguage.French,
            CreatedAtUtc,
            hasAcceptedLearningContract: true));

        Assert.Equal("localId", exception.ParamName);
    }
}
