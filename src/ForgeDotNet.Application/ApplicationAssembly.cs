using ForgeDotNet.Domain;

namespace ForgeDotNet.Application;

/// <summary>
/// Identifies the application assembly without introducing a use case.
/// </summary>
public static class ApplicationAssembly
{
    public static Type DomainAssemblyType => typeof(DomainAssembly);
}
