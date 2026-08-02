using ForgeDotNet.Application.Content;
using Microsoft.Extensions.DependencyInjection;

namespace ForgeDotNet.Infrastructure.Content;

public static class ContentValidationServiceCollectionExtensions
{
    public static IServiceCollection AddForgeContentValidation(
        this IServiceCollection services,
        ContentValidationOptions options)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(options);

        services.AddSingleton(options);
        services.AddSingleton<IContentValidationService, FileSystemContentValidationService>();
        services.AddSingleton<ValidateContentDirectory>();
        return services;
    }
}
