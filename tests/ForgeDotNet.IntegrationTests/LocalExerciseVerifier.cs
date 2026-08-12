using System.Collections.Immutable;
using System.Reflection;
using System.Runtime.Loader;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;

namespace ForgeDotNet.IntegrationTests;

/// <summary>
/// Exécute une suite d'exercice en mémoire, sans moteur Docker, pour prouver qu'une solution passe
/// ses cas et qu'un starter en échoue au moins un.
/// </summary>
/// <remarks>
/// <para>
/// <b>Frontière de sécurité.</b> Ce vérificateur compile et exécute du code de contenu
/// <em>dans le processus de test</em>. C'est acceptable ici pour deux raisons : ce code est versionné
/// et relu dans le dépôt, et le projet de tests compile déjà <c>content/sql/ef-*/**.cs</c> dans son
/// propre assemblage. Il ne doit <b>jamais</b> servir à exécuter une soumission d'apprenant : seul le
/// bac à sable Docker offre les garanties d'isolation, de quotas et de nettoyage requises pour cela.
/// </para>
/// <para>
/// La sémantique reproduit fidèlement <c>RunnerHost/Program.cs</c> : mêmes types autorisés, même
/// résolution de méthode, même désérialisation des arguments, même comparaison par
/// <see cref="JsonNode.DeepEquals"/>, même traitement de l'exception attendue et de la non-mutation
/// des entrées. Toute divergence rendrait la vérification locale trompeuse.
/// </para>
/// </remarks>
internal static class LocalExerciseVerifier
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    /// <summary>Copie de <c>RunnerTypeCatalog</c> : aucun autre type n'est exécutable.</summary>
    private static readonly Dictionary<string, Type> RunnerTypes = new(StringComparer.Ordinal)
    {
        ["bool"] = typeof(bool),
        ["date"] = typeof(DateOnly),
        ["decimal"] = typeof(decimal),
        ["dictionary<string,int>"] = typeof(Dictionary<string, int>),
        ["int"] = typeof(int),
        ["int[]"] = typeof(int[]),
        ["list<int>"] = typeof(List<int>),
        ["string"] = typeof(string),
    };

    private static readonly ImmutableArray<MetadataReference> References = LoadReferences();

    public static ExerciseSuite LoadSuite(string exerciseDirectory)
    {
        using JsonDocument runner = JsonDocument.Parse(
            File.ReadAllText(Path.Combine(exerciseDirectory, "tests", "runner.json")));
        JsonElement root = runner.RootElement;

        var cases = new List<ExerciseCase>();
        cases.AddRange(LoadCases(Path.Combine(exerciseDirectory, "tests", "visible", "cases.json"), isVisible: true));
        cases.AddRange(LoadCases(Path.Combine(exerciseDirectory, "tests", "hidden", "cases.json"), isVisible: false));

        return new ExerciseSuite(
            root.GetProperty("typeName").GetString()!,
            root.GetProperty("methodName").GetString()!,
            root.GetProperty("parameterTypes").EnumerateArray().Select(item => item.GetString()!).ToArray(),
            root.GetProperty("returnType").GetString()!,
            cases);
    }

    public static ExerciseRunOutcome Run(string source, ExerciseSuite suite, string assemblyName)
    {
        SyntaxTree tree = CSharpSyntaxTree.ParseText(
            source,
            new CSharpParseOptions(LanguageVersion.Latest));

        // Mêmes options que la chaîne du conteneur : bibliothèque, nullable actif, optimisations.
        var compilation = CSharpCompilation.Create(
            assemblyName,
            [tree],
            References,
            new CSharpCompilationOptions(
                OutputKind.DynamicallyLinkedLibrary,
                nullableContextOptions: NullableContextOptions.Enable,
                optimizationLevel: OptimizationLevel.Release));

        using var assemblyStream = new MemoryStream();
        EmitResult emit = compilation.Emit(assemblyStream);
        if (!emit.Success)
        {
            return new ExerciseRunOutcome(
                Compiled: false,
                emit.Diagnostics
                    .Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
                    .Select(diagnostic => diagnostic.ToString())
                    .ToArray(),
                []);
        }

        // Contexte déchargeable : cent trente-cinq assemblages ne restent pas en mémoire.
        var context = new AssemblyLoadContext(assemblyName, isCollectible: true);
        try
        {
            assemblyStream.Position = 0;
            Assembly assembly = context.LoadFromStream(assemblyStream);
            return new ExerciseRunOutcome(Compiled: true, [], RunCases(assembly, suite));
        }
        finally
        {
            context.Unload();
        }
    }

    private static List<string> RunCases(Assembly assembly, ExerciseSuite suite)
    {
        Type[] parameterTypes = suite.ParameterTypes.Select(Resolve).ToArray();
        Type returnType = Resolve(suite.ReturnType);

        Type? type = assembly.GetType(suite.TypeName, throwOnError: false, ignoreCase: false);
        if (type is null)
        {
            return [$"Type « {suite.TypeName} » introuvable dans l'assemblage compilé."];
        }

        MethodInfo? method = type.GetMethod(
            suite.MethodName,
            BindingFlags.Public | BindingFlags.Static,
            binder: null,
            types: parameterTypes,
            modifiers: null);
        if (method is null)
        {
            return [$"Méthode statique publique « {suite.MethodName} » absente ou de signature différente."];
        }

        if (method.ReturnType != returnType)
        {
            return [$"Type de retour {method.ReturnType.Name} au lieu de {returnType.Name}."];
        }

        var failures = new List<string>();
        foreach (ExerciseCase testCase in suite.Cases)
        {
            string? failure = RunCase(method, parameterTypes, returnType, testCase);
            if (failure is not null)
            {
                failures.Add($"{(testCase.IsVisible ? "visible" : "caché")} {testCase.Name} : {failure}");
            }
        }

        return failures;
    }

    private static string? RunCase(
        MethodInfo method,
        Type[] parameterTypes,
        Type returnType,
        ExerciseCase testCase)
    {
        object?[] arguments;
        try
        {
            arguments = testCase.Arguments.EnumerateArray()
                .Select((item, index) => item.Deserialize(parameterTypes[index], JsonOptions))
                .ToArray();
        }
        catch (JsonException exception)
        {
            return $"arguments illisibles ({exception.Message}).";
        }

        object? result;
        try
        {
            result = method.Invoke(null, arguments);
        }
        catch (TargetInvocationException exception) when (exception.InnerException is not null)
        {
            string thrown = exception.InnerException.GetType().Name;
            return string.Equals(testCase.ExpectedException, thrown, StringComparison.Ordinal)
                ? null
                : testCase.ExpectedException is null
                    ? $"exception inattendue {thrown}."
                    : $"exception {thrown} au lieu de {testCase.ExpectedException}.";
        }

        if (testCase.ExpectedException is not null)
        {
            return $"aucune exception levée alors que {testCase.ExpectedException} était attendue.";
        }

        JsonNode? expectedNode = JsonNode.Parse(JsonSerializer.Serialize(
            testCase.Expected.Deserialize(returnType, JsonOptions),
            returnType,
            JsonOptions));
        JsonNode? actualNode = JsonNode.Parse(JsonSerializer.Serialize(result, returnType, JsonOptions));
        if (!JsonNode.DeepEquals(expectedNode, actualNode))
        {
            return $"attendu {expectedNode?.ToJsonString()}, obtenu {actualNode?.ToJsonString()}.";
        }

        if (!testCase.ArgumentsUnchanged)
        {
            return null;
        }

        JsonNode? before = JsonNode.Parse(testCase.Arguments.GetRawText());
        JsonNode? after = JsonNode.Parse(JsonSerializer.Serialize(arguments, JsonOptions));
        return JsonNode.DeepEquals(before, after)
            ? null
            : "les entrées reçues ont été modifiées alors que le cas l'interdit.";
    }

    private static IEnumerable<ExerciseCase> LoadCases(string path, bool isVisible)
    {
        if (!File.Exists(path))
        {
            yield break;
        }

        using JsonDocument document = JsonDocument.Parse(File.ReadAllText(path));
        foreach (JsonElement item in document.RootElement.GetProperty("cases").EnumerateArray())
        {
            bool hasExpectedException =
                item.TryGetProperty("expectedException", out JsonElement expectedException)
                && expectedException.ValueKind == JsonValueKind.String;

            // Même exigence que FileSystemDockerRunSpecificationSource : un cas déclare un résultat
            // attendu OU une exception attendue, jamais les deux ni aucun des deux. Sans ce contrôle,
            // la vérification locale accepterait un cas que le vrai runner refuse.
            bool hasExpected = item.TryGetProperty("expected", out _);
            if (hasExpected == hasExpectedException)
            {
                throw new InvalidDataException(
                    $"Le cas « {item.GetProperty("name").GetString()} » de {path} doit déclarer "
                    + "exactement un résultat attendu ou une exception attendue.");
            }

            yield return new ExerciseCase(
                item.GetProperty("name").GetString()!,
                isVisible,
                item.GetProperty("arguments").Clone(),
                item.TryGetProperty("expected", out JsonElement expected) ? expected.Clone() : default,
                hasExpectedException ? expectedException.GetString() : null,
                item.TryGetProperty("argumentsUnchanged", out JsonElement unchanged)
                    && unchanged.ValueKind == JsonValueKind.True);
        }
    }

    private static Type Resolve(string name) => RunnerTypes.TryGetValue(name, out Type? type)
        ? type
        : throw new InvalidDataException($"Type de suite runner non autorisé : {name}.");

    private static ImmutableArray<MetadataReference> LoadReferences() =>
        ((string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") ?? string.Empty)
            .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
            .Where(path => path.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
            .Select(path => (MetadataReference)MetadataReference.CreateFromFile(path))
            .ToImmutableArray();
}

internal sealed record ExerciseSuite(
    string TypeName,
    string MethodName,
    IReadOnlyList<string> ParameterTypes,
    string ReturnType,
    IReadOnlyList<ExerciseCase> Cases);

internal sealed record ExerciseCase(
    string Name,
    bool IsVisible,
    JsonElement Arguments,
    JsonElement Expected,
    string? ExpectedException,
    bool ArgumentsUnchanged);

internal sealed record ExerciseRunOutcome(
    bool Compiled,
    IReadOnlyList<string> CompilerErrors,
    IReadOnlyList<string> FailedCases);
