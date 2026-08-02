using ForgeDotNet.Application;
using ForgeDotNet.Domain;

namespace ForgeDotNet.Infrastructure;

/// <summary>
/// Identifies the infrastructure assembly without introducing an adapter.
/// </summary>
public static class InfrastructureAssembly
{
    public static IReadOnlyCollection<Type> DependencyAssemblyTypes { get; } =
        [typeof(ApplicationAssembly), typeof(DomainAssembly)];
}
