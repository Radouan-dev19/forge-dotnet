using ForgeDotNet.Application.Content;
using Microsoft.Extensions.DependencyInjection;

namespace ForgeDotNet.Infrastructure.Content;

public static class ContentCatalogServiceCollectionExtensions
{
    public static IServiceCollection AddForgeContentCatalog(
        this IServiceCollection services,
        ContentValidationOptions options)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(options);

        services.AddForgeContentValidation(options);
        services.AddSingleton<IContentCatalogLoader, FileSystemContentCatalogLoader>();
        services.AddSingleton<ContentCatalogProvider>();
        return services;
    }
}
