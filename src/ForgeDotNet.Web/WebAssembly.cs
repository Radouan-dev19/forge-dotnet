using ForgeDotNet.Application;
using ForgeDotNet.CodeRunner;
using ForgeDotNet.Infrastructure;

namespace ForgeDotNet.Web;

/// <summary>
/// Identifies the web host assembly for structural tests.
/// </summary>
public static class WebAssembly
{
    public static IReadOnlyCollection<Type> CompositionAssemblyTypes { get; } =
        [typeof(ApplicationAssembly), typeof(CodeRunnerAssembly), typeof(InfrastructureAssembly)];
}
