using System.Diagnostics;
using ForgeDotNet.Application.Analytics;
using ForgeDotNet.Application.CodeRunner;
using ForgeDotNet.Application.Content;
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
using ForgeDotNet.CodeRunner;
using ForgeDotNet.Domain.Content;
using ForgeDotNet.Infrastructure.Content;
using ForgeDotNet.Infrastructure.Curriculum;
using ForgeDotNet.Infrastructure.DebugLab;
using ForgeDotNet.Infrastructure.Diagnostic;
using ForgeDotNet.Infrastructure.Exams;
using ForgeDotNet.Infrastructure.Persistence;
using ForgeDotNet.Infrastructure.Practice;
using ForgeDotNet.Infrastructure.SqlLab;
using ForgeDotNet.Infrastructure.WeeklyPlanning;
using ForgeDotNet.Web.Components;
using ForgeDotNet.Web.Health;
using Microsoft.AspNetCore.DataProtection;

const string MigrateOnlyArgument = "--migrate-only";
const string ValidateContentArgument = "--validate-content";
const string LoadCatalogArgument = "--load-catalog";
if (args.Contains(ValidateContentArgument, StringComparer.Ordinal))
{
    Environment.ExitCode = await RunContentValidationAsync(args);
    return;
}

if (args.Contains(LoadCatalogArgument, StringComparer.Ordinal))
{
    Environment.ExitCode = await RunCatalogAsync(args);
    return;
}

var migrateOnly = args.Contains(MigrateOnlyArgument, StringComparer.Ordinal);
var hostArguments = args
    .Where(argument => !string.Equals(argument, MigrateOnlyArgument, StringComparison.Ordinal))
    .ToArray();
var builder = WebApplication.CreateBuilder(hostArguments);

// Add services to the container.
builder.Logging.ClearProviders();
builder.Logging.AddJsonConsole();
builder.Services.AddRazorComponents().AddInteractiveServerComponents();
var localDataPaths = LocalDataPaths.Create(new LocalDataOptions
{
    DirectoryPath = builder.Configuration["LocalData:DirectoryPath"],
    DatabaseFileName = builder.Configuration["LocalData:DatabaseFileName"]
        ?? LocalDataOptions.DefaultDatabaseFileName,
});
localDataPaths.EnsureOutside(builder.Environment.ContentRootPath, builder.Environment.WebRootPath);
var dataProtectionDirectory = Path.Combine(localDataPaths.DataDirectory, "data-protection");
Directory.CreateDirectory(dataProtectionDirectory);
var dataProtection = builder.Services
    .AddDataProtection()
    .SetApplicationName("Forge.NET")
    .PersistKeysToFileSystem(new DirectoryInfo(dataProtectionDirectory));
if (OperatingSystem.IsWindows())
{
    dataProtection.ProtectKeysWithDpapi();
}

builder.Services.AddForgeLocalPersistence(localDataPaths);
string repositoryRoot = FindRepositoryRoot(builder.Environment.ContentRootPath)
    ?? FindRepositoryRoot(Directory.GetCurrentDirectory())
    ?? builder.Environment.ContentRootPath;
string contentRoot = ResolveConfiguredPath(
    builder.Configuration["Content:RootPath"],
    Path.Combine(repositoryRoot, "content"),
    builder.Environment.ContentRootPath);
string catalogDirectory = ResolveConfiguredPath(
    builder.Configuration["Content:CatalogDirectoryPath"],
    Path.Combine(contentRoot, "reference"),
    builder.Environment.ContentRootPath);
var contentOptions = new ContentValidationOptions { ContentRootPath = contentRoot };
builder.Services.AddForgeContentCatalog(contentOptions);
builder.Services.AddSingleton(new PracticeContentOptions
{
    ContentRootPath = contentRoot,
    CatalogDirectoryPath = catalogDirectory,
});
builder.Services.AddSingleton<IPracticeExerciseSource, FileSystemPracticeExerciseSource>();
builder.Services.AddSingleton<PracticeCoordinator>();
builder.Services.AddScoped<PracticeService>();
builder.Services.AddSingleton(new DebugContentOptions
{
    ContentRootPath = contentRoot,
    CatalogDirectoryPath = catalogDirectory,
});
builder.Services.AddSingleton<IDebugScenarioSource, FileSystemDebugScenarioSource>();
builder.Services.AddSingleton<DebugLabCoordinator>();
builder.Services.AddScoped<DebugLabService>();
string sqlLabSecretFile = ResolveConfiguredPath(
    builder.Configuration["SqlLab:AdministratorPasswordFile"],
    Path.Combine(repositoryRoot, ".secrets", "sql-lab-sa-password.txt"),
    builder.Environment.ContentRootPath);
var sqlLabOptions = new SqlLabOptions
{
    Enabled = builder.Configuration.GetValue("SqlLab:Enabled", false),
    Server = builder.Configuration["SqlLab:Server"] ?? "127.0.0.1",
    Port = builder.Configuration.GetValue("SqlLab:Port", 14333),
    AdministratorUser = builder.Configuration["SqlLab:AdministratorUser"] ?? "sa",
    AdministratorPasswordFile = sqlLabSecretFile,
    Encrypt = builder.Configuration.GetValue("SqlLab:Encrypt", true),
    TrustServerCertificate = builder.Configuration.GetValue("SqlLab:TrustServerCertificate", true),
    ConnectTimeoutSeconds = builder.Configuration.GetValue("SqlLab:ConnectTimeoutSeconds", 5),
    QueryTimeoutSeconds = builder.Configuration.GetValue("SqlLab:QueryTimeoutSeconds", 3),
    MaximumRows = builder.Configuration.GetValue("SqlLab:MaximumRows", 100),
    MaximumResultBytes = builder.Configuration.GetValue("SqlLab:MaximumResultBytes", 65_536),
    MaximumQueryCharacters = builder.Configuration.GetValue("SqlLab:MaximumQueryCharacters", 16_384),
    MaximumSessions = builder.Configuration.GetValue("SqlLab:MaximumSessions", 4),
    MaximumConcurrency = builder.Configuration.GetValue("SqlLab:MaximumConcurrency", 2),
};
sqlLabOptions.Validate();
builder.Services.AddSingleton(sqlLabOptions);
builder.Services.AddSingleton<SqlServerLabGateway>();
builder.Services.AddSingleton<ISqlLabGateway>(services => services.GetRequiredService<SqlServerLabGateway>());
builder.Services.AddScoped<SqlLabService>();
builder.Services.AddScoped<MasteryService>();
builder.Services.AddScoped<ReviewService>();
builder.Services.AddScoped<DashboardService>();
string examBankDirectory = ResolveConfiguredPath(
    builder.Configuration["Exams:BankDirectoryPath"],
    Path.Combine(contentRoot, "exams"),
    builder.Environment.ContentRootPath);
builder.Services.AddSingleton(new ExamBankOptions
{
    ContentRootPath = contentRoot,
    BankDirectoryPath = examBankDirectory,
});
builder.Services.AddSingleton<FileSystemExamBankSource>();
builder.Services.AddSingleton<IExamBankSource>(services => services.GetRequiredService<FileSystemExamBankSource>());
builder.Services.AddSingleton<IExamSqlItemSource>(services => services.GetRequiredService<FileSystemExamBankSource>());
builder.Services.AddSingleton<ISqlExamRunner, SqlLabExamRunner>();
builder.Services.AddScoped<ExamService>();
CodeRunnerMode codeRunnerMode = CodeRunnerModeParser.Parse(builder.Configuration["CodeRunner:Mode"]);
builder.Services.AddSingleton<IManualCodeRunPackageExporter, ManualCodeRunPackageExporter>();
builder.Services.AddSingleton(new DockerRunContentOptions
{
    ContentRootPath = contentRoot,
    CatalogDirectoryPath = catalogDirectory,
    SqlDirectoryPath = Path.Combine(contentRoot, "sql"),
});
builder.Services.AddSingleton<IDockerRunSpecificationSource, FileSystemDockerRunSpecificationSource>();
switch (codeRunnerMode)
{
    case CodeRunnerMode.Manual:
        builder.Services.AddSingleton<ICodeRunner, UnavailableCodeRunner>();
        break;
    case CodeRunnerMode.Deterministic:
        var deterministicRunnerOptions = new DeterministicCodeRunnerOptions
        {
            Scenarios = DeterministicCodeRunnerOptions.ParseScenarios(
                builder.Configuration["CodeRunner:DeterministicScenarios"]),
            Delay = TimeSpan.FromMilliseconds(
                builder.Configuration.GetValue("CodeRunner:DeterministicDelayMilliseconds", 0)),
        };
        deterministicRunnerOptions.Validate();
        builder.Services.AddSingleton(deterministicRunnerOptions);
        builder.Services.AddSingleton<ICodeRunner>(services => new DeterministicCodeRunner(
            deterministicRunnerOptions,
            services.GetRequiredService<TimeProvider>()));
        break;
    case CodeRunnerMode.Docker:
        var dockerRunnerOptions = new DockerCodeRunnerOptions
        {
            DockerExecutablePath = builder.Configuration["CodeRunner:Docker:ExecutablePath"] ?? "docker",
            DockerContext = builder.Configuration["CodeRunner:Docker:Context"] ?? "desktop-linux",
            ImageReference = builder.Configuration["CodeRunner:Docker:ImageReference"] ?? string.Empty,
            WorkspaceRootPath = builder.Configuration["CodeRunner:Docker:WorkspaceRootPath"]
                ?? Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "ForgeDotNet",
                    "runner-workspaces"),
            MaximumConcurrency = builder.Configuration.GetValue("CodeRunner:Docker:MaximumConcurrency", 2),
            CpuCount = builder.Configuration.GetValue("CodeRunner:Docker:CpuCount", 0.5),
            MemoryBytes = builder.Configuration.GetValue(
                "CodeRunner:Docker:MemoryBytes",
                512L * DockerCodeRunnerOptions.Mebibyte),
            PidsLimit = builder.Configuration.GetValue("CodeRunner:Docker:PidsLimit", 64),
            WorkspaceBytes = builder.Configuration.GetValue(
                "CodeRunner:Docker:WorkspaceBytes",
                64L * DockerCodeRunnerOptions.Mebibyte),
            CompilationTimeout = TimeSpan.FromSeconds(
                builder.Configuration.GetValue("CodeRunner:Docker:CompilationTimeoutSeconds", 25)),
            TestTimeout = TimeSpan.FromSeconds(
                builder.Configuration.GetValue("CodeRunner:Docker:TestTimeoutSeconds", 15)),
            DockerControlTimeout = TimeSpan.FromSeconds(
                builder.Configuration.GetValue("CodeRunner:Docker:ControlTimeoutSeconds", 15)),
        };
        dockerRunnerOptions.Validate();
        builder.Services.AddSingleton(dockerRunnerOptions);
        builder.Services.AddSingleton<ICodeRunner>(services => new DockerCodeRunner(
            dockerRunnerOptions,
            services.GetRequiredService<IDockerRunSpecificationSource>(),
            services.GetRequiredService<TimeProvider>()));
        break;
    default:
        throw new InvalidDataException("Le mode CodeRunner configuré n’est pas autorisé.");
}
builder.Services.AddSingleton<RunExerciseHistory>();
builder.Services.AddScoped<RunExercise>();
builder.Services.AddSingleton(new LessonContentOptions
{
    ContentRootPath = contentRoot,
    CatalogDirectoryPath = catalogDirectory,
    CurriculumId = builder.Configuration["Content:CurriculumId"] ?? "forge-reference",
});
builder.Services.AddSingleton<ILessonContentSource, FileSystemLessonContentSource>();
builder.Services.AddScoped<BrowseLessons>();
builder.Services.AddScoped<GetLessonReaderState>();
builder.Services.AddScoped<SaveLessonNote>();
builder.Services.AddScoped<SetLessonBookmark>();
builder.Services.AddScoped<RecordLessonSectionRead>();
builder.Services.AddScoped<SubmitLessonQuiz>();
string diagnosticBankDirectory = ResolveConfiguredPath(
    builder.Configuration["Diagnostic:BankDirectoryPath"],
    Path.Combine(contentRoot, "diagnostic", "v1"),
    builder.Environment.ContentRootPath);
builder.Services.AddSingleton(new DiagnosticBankOptions
{
    ContentRootPath = contentRoot,
    BankDirectoryPath = diagnosticBankDirectory,
});
builder.Services.AddSingleton<FileSystemDiagnosticBankSource>();
builder.Services.AddSingleton<IDiagnosticBankSource>(services =>
    services.GetRequiredService<FileSystemDiagnosticBankSource>());
builder.Services.AddSingleton<IDiagnosticRubricSource>(services =>
    services.GetRequiredService<FileSystemDiagnosticBankSource>());
builder.Services.AddSingleton(new DiagnosticSessionOptions
{
    InitialSectionDuration = TimeSpan.FromMinutes(
        builder.Configuration.GetValue("Diagnostic:InitialSectionDurationMinutes", 30)),
    ReducedSectionDuration = TimeSpan.FromSeconds(
        builder.Configuration.GetValue("Diagnostic:ReducedSectionDurationSeconds", 120)),
});
builder.Services.AddSingleton<DiagnosticSessionCoordinator>();
builder.Services.AddScoped<DiagnosticSessionService>();
builder.Services.AddScoped<DiagnosticEvaluationService>();
string weeklyPlanCurriculumDirectory = ResolveConfiguredPath(
    builder.Configuration["WeeklyPlan:CurriculumDirectoryPath"],
    Path.Combine(contentRoot, "planning", "v1"),
    builder.Environment.ContentRootPath);
builder.Services.AddSingleton(new WeeklyPlanCurriculumOptions
{
    ContentRootPath = contentRoot,
    DirectoryPath = weeklyPlanCurriculumDirectory,
});
builder.Services.AddSingleton<IWeeklyPlanCurriculumSource, FileSystemWeeklyPlanCurriculumSource>();
builder.Services.AddSingleton<WeeklyPlanCoordinator>();
builder.Services.AddScoped<WeeklyPlanService>();
builder.Services.AddScoped<GetLocalProfile>();
builder.Services.AddScoped<UpdateLocalProfile>();
builder.Services.AddScoped<SetLearningContractAcceptance>();
builder.Services.AddHealthChecks()
    .AddCheck<LocalProfileHealthCheck>("local-profile")
    .AddCheck<SqlLabHealthCheck>("sql-lab");

var app = builder.Build();

await InitializeReaderContentAsync(app.Services, catalogDirectory);
_ = await app.Services.GetRequiredService<IPracticeExerciseSource>().ListAsync();
_ = await app.Services.GetRequiredService<IDebugScenarioSource>().ListAsync();
_ = await app.Services.GetRequiredService<IDiagnosticBankSource>().GetAsync();
_ = await app.Services.GetRequiredService<IDiagnosticRubricSource>().GetRubricAsync();
_ = await app.Services.GetRequiredService<IWeeklyPlanCurriculumSource>().GetAsync();
_ = await app.Services.GetRequiredService<IExamBankSource>().ListAsync();

if (app.Environment.IsDevelopment() || migrateOnly)
{
    await app.Services.GetRequiredService<LocalDatabaseInitializer>().MigrateAsync();
}

if (migrateOnly)
{
    return;
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.Use(async (context, next) =>
{
    context.Response.Headers.ContentSecurityPolicy =
        "default-src 'self'; base-uri 'self'; connect-src 'self' ws: wss:; "
        + "font-src 'self'; form-action 'self'; frame-ancestors 'none'; img-src 'self' data:; "
        + "object-src 'none'; script-src 'self'; style-src 'self'";
    context.Response.Headers.XContentTypeOptions = "nosniff";
    context.Response.Headers["Referrer-Policy"] = "no-referrer";
    await next();
});
if (builder.Configuration.GetValue("Web:UseHttpsRedirection", true))
{
    app.UseHttpsRedirection();
}

app.UseAntiforgery();

app.MapStaticAssets();
app.MapHealthChecks("/health");
app.MapHealthChecks("/health/sql-lab", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = registration => string.Equals(registration.Name, "sql-lab", StringComparison.Ordinal),
});
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

app.Run();

static async Task<int> RunContentValidationAsync(string[] arguments)
{
    int argumentIndex = Array.IndexOf(arguments, ValidateContentArgument);
    if (argumentIndex < 0 || argumentIndex + 1 >= arguments.Length || arguments.Length != 2)
    {
        Console.Error.WriteLine("Usage : --validate-content <dossier-sous-content>");
        return 2;
    }

    string? repositoryRoot = FindRepositoryRoot(Directory.GetCurrentDirectory());
    if (repositoryRoot is null)
    {
        Console.Error.WriteLine("Racine du dépôt introuvable : exécuter la commande depuis Forge.NET.");
        return 2;
    }

    string contentRoot = Path.Combine(repositoryRoot, "content");
    string requestedPath = Path.IsPathRooted(arguments[argumentIndex + 1])
        ? Path.GetFullPath(arguments[argumentIndex + 1])
        : Path.GetFullPath(arguments[argumentIndex + 1], repositoryRoot);
    var validationService = new FileSystemContentValidationService(
        new ContentValidationOptions { ContentRootPath = contentRoot });
    var validateContent = new ValidateContentDirectory(validationService);
    ContentValidationReport report = await validateContent.ExecuteAsync(requestedPath);

    foreach (ContentValidationIssue issue in report.Issues)
    {
        Console.Error.WriteLine($"{issue.FilePath} | {issue.PropertyPath} | {issue.Code} | {issue.Message}");
    }

    string status = report.IsValid ? "VALIDE" : "INVALIDE";
    Console.WriteLine(
        $"{status} : {report.DocumentsExamined} document(s), {report.FilesExamined} fichier(s), "
        + $"{report.Issues.Count} erreur(s), {report.AcceptedDocuments} document(s) accepté(s).");
    return report.IsValid ? 0 : 1;
}

static async Task<int> RunCatalogAsync(string[] arguments)
{
    if (arguments.Length < 2 || arguments[0] != LoadCatalogArgument || arguments.Length % 2 != 0)
    {
        Console.Error.WriteLine(
            "Usage : --load-catalog <dossier> [--search <texte>] [--skill <id>] [--reload-from <dossier>]");
        return 2;
    }

    string? repositoryRoot = FindRepositoryRoot(Directory.GetCurrentDirectory());
    if (repositoryRoot is null)
    {
        Console.Error.WriteLine("Racine du dépôt introuvable : exécuter la commande depuis Forge.NET.");
        return 2;
    }

    string? search = null;
    string? skill = null;
    string? reloadFrom = null;
    for (int index = 2; index < arguments.Length; index += 2)
    {
        string value = arguments[index + 1];
        switch (arguments[index])
        {
            case "--search":
                search = value;
                break;
            case "--skill":
                skill = value;
                break;
            case "--reload-from":
                reloadFrom = value;
                break;
            default:
                Console.Error.WriteLine($"Option de catalogue inconnue : {arguments[index]}.");
                return 2;
        }
    }

    string contentRoot = Path.Combine(repositoryRoot, "content");
    var options = new ContentValidationOptions { ContentRootPath = contentRoot };
    var validationService = new FileSystemContentValidationService(options);
    var loader = new FileSystemContentCatalogLoader(validationService, options);
    using var provider = new ContentCatalogProvider(loader);
    var stopwatch = Stopwatch.StartNew();
    ContentCatalogReloadResult initial = await provider.ReloadAsync(
        ResolveRepositoryPath(arguments[1], repositoryRoot));
    stopwatch.Stop();
    if (!initial.Succeeded)
    {
        WriteIssues(initial.Issues);
        Console.WriteLine(
            $"CATALOGUE REFUSE : {initial.Issues.Count} erreur(s), snapshot={provider.Current.Revision}.");
        return 1;
    }

    ContentCatalog catalog = provider.Current;
    Console.WriteLine(
        $"CATALOGUE CHARGE : revision={catalog.Revision}, documents={catalog.Items.Count}, "
        + $"types={catalog.Items.Select(item => item.Type).Distinct().Count()}, elapsedMs={stopwatch.ElapsedMilliseconds}.");
    if (search is not null || skill is not null)
    {
        IReadOnlyList<ContentCatalogItem> results = catalog.Search(search ?? string.Empty, skill: skill);
        Console.WriteLine($"RECHERCHE : résultats={results.Count}, texte='{search ?? string.Empty}', compétence='{skill ?? string.Empty}'.");
        foreach (ContentCatalogItem item in results)
        {
            Console.WriteLine($"{item.Id} | {item.Type} | {item.Title}");
        }
    }

    if (reloadFrom is null)
    {
        return 0;
    }

    string previousRevision = catalog.Revision;
    ContentCatalogReloadResult reload = await provider.ReloadAsync(
        ResolveRepositoryPath(reloadFrom, repositoryRoot));
    if (!reload.Succeeded)
    {
        WriteIssues(reload.Issues);
        Console.WriteLine(
            $"RECHARGEMENT REFUSE : erreurs={reload.Issues.Count}, "
            + $"snapshotPréservé={reload.PreviousSnapshotPreserved && provider.Current.Revision == previousRevision}.");
        return 1;
    }

    Console.WriteLine($"RECHARGEMENT REUSSI : revision={provider.Current.Revision}.");
    return 0;
}

static string ResolveRepositoryPath(string path, string repositoryRoot) =>
    Path.IsPathRooted(path)
        ? Path.GetFullPath(path)
        : Path.GetFullPath(path, repositoryRoot);

static string ResolveConfiguredPath(
    string? configuredPath,
    string defaultPath,
    string contentRootPath) => string.IsNullOrWhiteSpace(configuredPath)
        ? Path.GetFullPath(defaultPath)
        : Path.IsPathFullyQualified(configuredPath)
            ? Path.GetFullPath(configuredPath)
            : Path.GetFullPath(configuredPath, contentRootPath);

static async Task InitializeReaderContentAsync(
    IServiceProvider services,
    string catalogDirectory)
{
    ContentCatalogProvider provider = services.GetRequiredService<ContentCatalogProvider>();
    ContentCatalogReloadResult reload = await provider.ReloadAsync(catalogDirectory);
    if (!reload.Succeeded)
    {
        throw new InvalidDataException(
            $"Le catalogue public du lecteur est invalide ({reload.Issues.Count} erreur(s)).");
    }

    ILessonContentSource source = services.GetRequiredService<ILessonContentSource>();
    LessonLibraryView library = await source.GetLibraryAsync();
    foreach (LessonSummaryView lesson in library.Modules.SelectMany(module => module.Lessons))
    {
        if (await source.GetLessonAsync(lesson.Id) is null)
        {
            throw new InvalidDataException("Une leçon du parcours public ne peut pas être chargée.");
        }
    }
}

static void WriteIssues(IEnumerable<ContentValidationIssue> issues)
{
    foreach (ContentValidationIssue issue in issues)
    {
        Console.Error.WriteLine($"{issue.FilePath} | {issue.PropertyPath} | {issue.Code} | {issue.Message}");
    }
}

static string? FindRepositoryRoot(string startPath)
{
    for (DirectoryInfo? directory = new(startPath); directory is not null; directory = directory.Parent)
    {
        if (File.Exists(Path.Combine(directory.FullName, "ForgeDotNet.sln"))
            && Directory.Exists(Path.Combine(directory.FullName, "content", "schemas")))
        {
            return directory.FullName;
        }
    }

    return null;
}

public partial class Program;
