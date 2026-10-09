using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using UniCore.Api.Database;

namespace UniCore.Api.Extensions;

public static class DatabaseServiceCollectionExtensions
{
    /// <summary>
    /// Inicializa la configuración usada por AppSettingsCache y registra los servicios auxiliares.
    /// </summary>
    public static IServiceCollection AddDatabaseInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        ConfigurationService.Initialize(configuration);
        services.AddScoped<PersistenceHealth>();

        return services;
    }
}
