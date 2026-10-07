using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using UniCore.Api.Database;

namespace UniCore.Api.Extensions;

public static class DatabaseServiceCollectionExtensions
{
    /// <summary>
    /// Registra la infraestructura de base de datos:
    /// <see cref="DatabaseProvider"/> y <see cref="DatabaseTransactionProvider"/> por ámbito
    /// de petición, e inicializa la configuración que consume la capa de datos estática.
    /// </summary>
    public static IServiceCollection AddDatabaseInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        ConfigurationService.Initialize(configuration);

        services.AddScoped<DatabaseProvider>();
        services.AddScoped<DatabaseTransactionProvider>();
        services.AddScoped<PersistenceHealth>();

        return services;
    }
}
