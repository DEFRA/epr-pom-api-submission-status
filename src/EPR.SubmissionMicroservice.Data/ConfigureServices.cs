namespace EPR.SubmissionMicroservice.Data;

using System.Diagnostics.CodeAnalysis;
using Common.Functions.Database.Context.Interfaces;
using Common.Functions.Extensions;
using Microsoft.Azure.Cosmos;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Options;
using Repositories.Commands;
using Repositories.Commands.Interfaces;
using Repositories.Queries;
using Repositories.Queries.Interfaces;

[ExcludeFromCodeCoverage]
public static class ConfigureServices
{
    public static IServiceCollection AddDataServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ConfigureOptions(services, configuration);
        var serviceProvider = services.BuildServiceProvider();
        return services
            .AddCommonServices()
            .AddCommonDatabaseServices()
            .RegisterRepositories()
            .RegisterCosmosDatabase(serviceProvider);
    }

    private static void ConfigureOptions(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<DatabaseOptions>(configuration.GetSection(DatabaseOptions.ConfigSection));
    }

    private static IServiceCollection RegisterCosmosDatabase(
        this IServiceCollection services,
        IServiceProvider serviceProvider)
    {
        var databaseOptions = serviceProvider.GetRequiredService<IOptions<DatabaseOptions>>().Value;
        return services.AddDbContext<IEprCommonContext, SubmissionContext>(
            options =>
            {
                options.UseCosmos(
                    databaseOptions.ConnectionString,
                    databaseOptions.AccountKey,
                    databaseOptions.Name,
                    c =>
                    {
                        c.ConnectionMode(ConnectionMode.Gateway);
                        c.ExecutionStrategy(x =>
                            new CosmosDbRetryExecutionStrategy(
                                x.CurrentContext.Context,
                                databaseOptions.MaxRetryCount,
                                TimeSpan.FromMilliseconds(databaseOptions.MaxRetryDelayInMilliseconds)));
                    });
                options.EnableSensitiveDataLogging();
            });
    }

    private static IServiceCollection RegisterRepositories(this IServiceCollection services) =>
        services
            .AddScoped(typeof(ICommandRepository<>), typeof(CommandRepository<>))
            .AddScoped(typeof(IQueryRepository<>), typeof(QueryRepository<>));
}