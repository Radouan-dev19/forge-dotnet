using ForgeDotNet.Application.Analytics;
using ForgeDotNet.Application.CodeRunner;
using ForgeDotNet.Application.Curriculum;
using ForgeDotNet.Application.DebugLab;
using ForgeDotNet.Application.Diagnostic;
using ForgeDotNet.Application.Exams;
using ForgeDotNet.Application.IdentityLocal;
using ForgeDotNet.Application.Mastery;
using ForgeDotNet.Application.Practice;
using ForgeDotNet.Application.Reviews;
using ForgeDotNet.Application.SqlLab;
using ForgeDotNet.Application.WeeklyPlanning;
using ForgeDotNet.Infrastructure.Analytics;
using ForgeDotNet.Infrastructure.Curriculum;
using ForgeDotNet.Infrastructure.DebugLab;
using ForgeDotNet.Infrastructure.Diagnostic;
using ForgeDotNet.Infrastructure.Exams;
using ForgeDotNet.Infrastructure.IdentityLocal;
using ForgeDotNet.Infrastructure.Mastery;
using ForgeDotNet.Infrastructure.Practice;
using ForgeDotNet.Infrastructure.Reviews;
using ForgeDotNet.Infrastructure.SqlLab;
using ForgeDotNet.Infrastructure.WeeklyPlanning;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ForgeDotNet.Infrastructure.Persistence;

public static class LocalPersistenceServiceCollectionExtensions
{
    public static IServiceCollection AddForgeLocalPersistence(
        this IServiceCollection services,
        LocalDataPaths paths)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(paths);

        Directory.CreateDirectory(paths.DataDirectory);
        services.AddSingleton(paths);
        services.AddSingleton<ILocalDataLocation>(paths);
        services.AddSingleton<LocalDatabaseGate>();
        services.TryAddSingleton(TimeProvider.System);
        services.AddDbContextFactory<ForgeDbContext>(options => options.UseSqlite(paths.ConnectionString));
        services.AddSingleton<LocalDatabaseInitializer>();
        services.AddSingleton<LocalDatabaseHealthProbe>();
        services.AddSingleton<ILocalProfileRepository, SqliteLocalProfileRepository>();
        services.AddSingleton<ILessonUserStateRepository, SqliteLessonUserStateRepository>();
        services.AddSingleton<IDiagnosticSessionRepository, SqliteDiagnosticSessionRepository>();
        services.AddSingleton<IDiagnosticEvaluationRepository, SqliteDiagnosticEvaluationRepository>();
        services.AddSingleton<IWeeklyPlanRepository, SqliteWeeklyPlanRepository>();
        services.AddSingleton<IPracticeActivityRepository, SqlitePracticeActivityRepository>();
        services.AddSingleton<IPracticeLearningAttemptRepository, SqlitePracticeLearningAttemptRepository>();
        services.AddSingleton<IDebugLabRepository, SqliteDebugLabRepository>();
        services.AddSingleton<ISqlLearningAttemptRepository, SqliteSqlLearningAttemptRepository>();
        services.AddSingleton<IMasteryEvidenceSource, SqliteMasteryEvidenceSource>();
        services.AddSingleton<IMasteryProjectionRepository, SqliteMasteryProjectionRepository>();
        services.AddSingleton<IMasteryPolicySource, VersionedMasteryPolicySource>();
        services.AddSingleton<IReviewRepository, SqliteReviewRepository>();
        services.AddSingleton<IReviewSourceProvider, SqliteReviewSourceProvider>();
        services.AddSingleton<IReviewPolicySource, VersionedReviewPolicySource>();
        services.AddSingleton<SqliteExamRepository>();
        services.AddSingleton<IExamRepository>(services => services.GetRequiredService<SqliteExamRepository>());
        services.AddSingleton<IExamAccessPolicy>(services => services.GetRequiredService<SqliteExamRepository>());
        services.AddSingleton<IAnalyticsEvidenceSource, SqliteAnalyticsEvidenceSource>();
        services.AddSingleton<ILocalDataBackupService, LocalDataBackupService>();
        return services;
    }
}
