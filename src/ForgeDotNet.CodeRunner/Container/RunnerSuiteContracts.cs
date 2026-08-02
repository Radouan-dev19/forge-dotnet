using System.Text.Json;

namespace ForgeDotNet.CodeRunner;

internal sealed record RunnerSuiteDefinition(
    int SchemaVersion,
    string SuiteId,
    string ExerciseId,
    int ExerciseVersion,
    string TypeName,
    string MethodName,
    IReadOnlyList<string> ParameterTypes,
    string ReturnType,
    IReadOnlyList<RunnerTestCase> Cases);

internal sealed record RunnerTestCase(
    string Name,
    string Message,
    bool IsVisible,
    JsonElement Arguments,
    bool HasExpectedResult,
    JsonElement Expected,
    string? ExpectedException,
    bool ArgumentsUnchanged);

internal sealed record RunnerCaseInvocation(
    string TypeName,
    string MethodName,
    IReadOnlyList<string> ParameterTypes,
    string ReturnType,
    JsonElement Arguments,
    bool CaptureArguments);

internal sealed record RunnerCaseResult(
    bool InfrastructureFailure,
    string? ResultJson,
    string? ExceptionType,
    string? ArgumentsJson);

internal static class RunnerTypeCatalog
{
    private static readonly Dictionary<string, Type> Types =
        new Dictionary<string, Type>(StringComparer.Ordinal)
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

    public static Type Resolve(string name) => Types.TryGetValue(name, out Type? type)
        ? type
        : throw new InvalidDataException("Un type de suite runner n’est pas autorisé.");
}
