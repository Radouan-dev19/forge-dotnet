using ForgeDotNet.Application.Curriculum;
using ForgeDotNet.Application.IdentityLocal;
using ForgeDotNet.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ForgeDotNet.Infrastructure.Curriculum;

public sealed class SqliteLessonUserStateRepository(
    IDbContextFactory<ForgeDbContext> contextFactory,
    LocalDatabaseGate databaseGate,
    TimeProvider timeProvider) : ILessonUserStateRepository
{
    public async ValueTask<LessonUserStateSnapshot> GetAsync(
        Guid profileId,
        string lessonId,
        CancellationToken cancellationToken = default)
    {
        ValidateKeys(profileId, lessonId);
        await using var lease = await databaseGate.AcquireAsync(cancellationToken);
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        string note = await context.LessonNotes
            .AsNoTracking()
            .Where(item => item.ProfileId == profileId && item.LessonId == lessonId)
            .Select(item => item.Text)
            .SingleOrDefaultAsync(cancellationToken)
            ?? string.Empty;
        bool bookmark = await context.LessonBookmarks
            .AsNoTracking()
            .Where(item => item.ProfileId == profileId && item.LessonId == lessonId)
            .Select(item => item.IsBookmarked)
            .SingleOrDefaultAsync(cancellationToken);
        string[] completed = await context.LessonReadingActivities
            .AsNoTracking()
            .Where(item => item.ProfileId == profileId && item.LessonId == lessonId)
            .OrderBy(item => item.ActivityId)
            .Select(item => item.ActivityId)
            .ToArrayAsync(cancellationToken);
        return new LessonUserStateSnapshot(note, bookmark, Array.AsReadOnly(completed));
    }

    public async ValueTask SaveNoteAsync(
        Guid profileId,
        string lessonId,
        string note,
        CancellationToken cancellationToken = default)
    {
        ValidateKeys(profileId, lessonId);
        ArgumentNullException.ThrowIfNull(note);
        if (note.Length > SaveLessonNote.MaximumNoteLength)
        {
            throw new ArgumentException("La note dépasse la taille autorisée.", nameof(note));
        }

        await using var lease = await databaseGate.AcquireAsync(cancellationToken);
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        LessonNoteRecord? record = await context.LessonNotes.SingleOrDefaultAsync(
            item => item.ProfileId == profileId && item.LessonId == lessonId,
            cancellationToken);
        if (record is null)
        {
            context.LessonNotes.Add(new LessonNoteRecord
            {
                ProfileId = profileId,
                LessonId = lessonId,
                Text = note,
                UpdatedAtUtc = timeProvider.GetUtcNow(),
            });
        }
        else
        {
            record.Text = note;
            record.UpdatedAtUtc = timeProvider.GetUtcNow();
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    public async ValueTask SetBookmarkAsync(
        Guid profileId,
        string lessonId,
        bool isBookmarked,
        CancellationToken cancellationToken = default)
    {
        ValidateKeys(profileId, lessonId);
        await using var lease = await databaseGate.AcquireAsync(cancellationToken);
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        LessonBookmarkRecord? record = await context.LessonBookmarks.SingleOrDefaultAsync(
            item => item.ProfileId == profileId && item.LessonId == lessonId,
            cancellationToken);
        if (record is null)
        {
            context.LessonBookmarks.Add(new LessonBookmarkRecord
            {
                ProfileId = profileId,
                LessonId = lessonId,
                IsBookmarked = isBookmarked,
                UpdatedAtUtc = timeProvider.GetUtcNow(),
            });
        }
        else
        {
            record.IsBookmarked = isBookmarked;
            record.UpdatedAtUtc = timeProvider.GetUtcNow();
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    public async ValueTask AddCompletedActivityAsync(
        Guid profileId,
        string lessonId,
        string activityId,
        CancellationToken cancellationToken = default)
    {
        ValidateKeys(profileId, lessonId);
        ArgumentException.ThrowIfNullOrWhiteSpace(activityId);
        if (activityId.Length > 160)
        {
            throw new ArgumentException("L'identifiant d'activité est trop long.", nameof(activityId));
        }

        await using var lease = await databaseGate.AcquireAsync(cancellationToken);
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        bool exists = await context.LessonReadingActivities.AnyAsync(
            item => item.ProfileId == profileId
                && item.LessonId == lessonId
                && item.ActivityId == activityId,
            cancellationToken);
        if (!exists)
        {
            context.LessonReadingActivities.Add(new LessonReadingActivityRecord
            {
                ProfileId = profileId,
                LessonId = lessonId,
                ActivityId = activityId,
                CompletedAtUtc = timeProvider.GetUtcNow(),
            });
            await context.SaveChangesAsync(cancellationToken);
        }
    }

    private static void ValidateKeys(Guid profileId, string lessonId)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(profileId, Guid.Empty);
        ArgumentException.ThrowIfNullOrWhiteSpace(lessonId);
        if (lessonId.Length > 120)
        {
            throw new ArgumentException("L'identifiant de leçon est trop long.", nameof(lessonId));
        }
    }
}
