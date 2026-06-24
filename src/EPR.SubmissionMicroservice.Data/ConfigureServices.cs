namespace EPR.SubmissionMicroservice.Data;

using System.Diagnostics.CodeAnalysis;
using Azure.Identity;
using Common.Functions.Database.Context.Interfaces;
using Common.Functions.Extensions;
using Microsoft.Azure.Cosmos;
using System.Net.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
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
        services.ConfigureOptions(configuration);
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
        var ignoreCertificateErrors = string.Equals(
            Environment.GetEnvironmentVariable("Database__IgnoreCertificateErrors"),
            "true",
            StringComparison.OrdinalIgnoreCase);

        // Created once and reused so EF Core's internal service provider cache stays stable:
        // a fresh TokenCredential per DbContext instantiation changes the options fingerprint
        // and triggers ManyServiceProvidersCreatedWarning after 20 contexts.
        var credential = string.IsNullOrWhiteSpace(databaseOptions.AccountKey)
            ? new DefaultAzureCredential()
            : null;

        return services.AddDbContext<IEprCommonContext, SubmissionContext>(
            options =>
            {
                if (credential is null)
                {
                    options.UseCosmos(
                        databaseOptions.ConnectionString,
                        databaseOptions.AccountKey,
                        databaseOptions.Name,
                        ConfigureCosmos);
                }
                else
                {
                    options.UseCosmos(
                        databaseOptions.ConnectionString,
                        credential,
                        databaseOptions.Name,
                        ConfigureCosmos);
                }
            });

        void ConfigureCosmos(CosmosDbContextOptionsBuilder c)
        {
            c.ConnectionMode(ConnectionMode.Gateway);
            if (ignoreCertificateErrors)
            {
                c.HttpClientFactory(() => new HttpClient(
                    new HttpClientHandler
                    {
                        ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator,
                    }));
            }

            c.ExecutionStrategy(x =>
                new CosmosDbRetryExecutionStrategy(
                    x.CurrentContext.Context,
                    databaseOptions.MaxRetryCount,
                    TimeSpan.FromMilliseconds(databaseOptions.MaxRetryDelayInMilliseconds)));
        }
    }

    private static IServiceCollection RegisterRepositories(this IServiceCollection services) =>
        services
            .AddScoped(typeof(ICommandRepository<>), typeof(CommandRepository<>))
            .AddScoped(typeof(IQueryRepository<>), typeof(QueryRepository<>))
            .AddScoped<ISubmissionCommandRepository, SubmissionCommandRepository>()
            .AddScoped<ISubmissionQueryRepository, SubmissionQueryRepository>();
}