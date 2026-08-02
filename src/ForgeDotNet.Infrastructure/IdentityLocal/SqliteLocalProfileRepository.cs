using ForgeDotNet.Application.IdentityLocal;
using ForgeDotNet.Domain.IdentityLocal;
using ForgeDotNet.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ForgeDotNet.Infrastructure.IdentityLocal;

public sealed class SqliteLocalProfileRepository(
    IDbContextFactory<ForgeDbContext> contextFactory,
    LocalDatabaseGate databaseGate,
    TimeProvider timeProvider) : ILocalProfileRepository
{
    public async ValueTask<UserProfile> GetAsync(CancellationToken cancellationToken = default)
    {
        await using var lease = await databaseGate.AcquireAsync(cancellationToken);
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        var storedProfile = await context.LocalProfiles
            .AsNoTracking()
            .SingleOrDefaultAsync(cancellationToken);

        if (storedProfile is not null)
        {
            return storedProfile.ToDomain();
        }

        var initialProfile = UserProfile.CreateDefault(timeProvider.GetUtcNow());
        context.LocalProfiles.Add(LocalProfileRecord.FromDomain(initialProfile));
        await context.SaveChangesAsync(cancellationToken);
        return initialProfile;
    }

    public async ValueTask SaveAsync(UserProfile profile, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(profile);

        await using var lease = await databaseGate.AcquireAsync(cancellationToken);
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        var storedProfile = await context.LocalProfiles.SingleOrDefaultAsync(cancellationToken);

        if (storedProfile is null)
        {
            context.LocalProfiles.Add(LocalProfileRecord.FromDomain(profile));
        }
        else
        {
            storedProfile.Apply(profile);
        }

        await context.SaveChangesAsync(cancellationToken);
    }
}
