using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using EPR.SubmissionMicroservice.Application.Behaviours;
using EPR.SubmissionMicroservice.Application.Features.Commands.SubmissionSubmit;
using EPR.SubmissionMicroservice.Application.Features.Queries.Common;
using EPR.SubmissionMicroservice.Application.Features.Queries.Common.Interfaces;
using EPR.SubmissionMicroservice.Application.Features.Queries.Common.Services;
using EPR.SubmissionMicroservice.Application.Features.Queries.Helpers;
using EPR.SubmissionMicroservice.Application.Features.Queries.Helpers.Interfaces;
using EPR.SubmissionMicroservice.Application.Features.Queries.SubmissionGet;
using EPR.SubmissionMicroservice.Application.Features.Queries.SubmissionsGet;
using EPR.SubmissionMicroservice.Application.Messaging.Publishing;
using EPR.SubmissionMicroservice.Application.Options;
using ErrorOr;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

[assembly: InternalsVisibleTo("EPR.SubmissionMicroservice.Application.UnitTests")]
namespace EPR.SubmissionMicroservice.Application;

[ExcludeFromCodeCoverage]
public static class ConfigureServices
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
    {
        ConfigureOptions(services, configuration);
        return services.AddBaseServices()
            .AddMessagingServices(configuration);
    }

    public static async Task ConfigureMessaging(this IServiceProvider serviceProvider, ILogger logger)
    {
        using (logger.BeginScope("Configuring messaging"))
        {
            try
            {
                var topicCreator = serviceProvider.GetRequiredService<IServiceBusTopicCreator>();
                await topicCreator.ConfigureTopics();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error while configuring messaging");
            }
        }
    }

    private static void ConfigureOptions(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<ValidationOptions>(configuration.GetSection(ValidationOptions.ConfigSection));
        services.Configure<ServiceBusOptions>(configuration.GetSection(ServiceBusOptions.ConfigSection));
    }

    private static IServiceCollection AddBaseServices(this IServiceCollection services) =>
        services
            .AddAutoMapper(Assembly.GetExecutingAssembly())
            .AddScoped<IPomSubmissionEventHelper, PomSubmissionEventHelper>()
            .AddScoped<IValidationEventHelper, ValidationEventHelper>()
            .AddScoped<IRegistrationSubmissionEventHelper, RegistrationSubmissionEventHelper>()
            .AddScoped<ISubsidiarySubmissionEventHelper, SubsidiarySubmissionEventHelper>()
            .AddScoped<ICompaniesHouseSubmissionEventHelper, CompaniesHouseSubmissionEventHelper>()
            .AddScoped<IAccreditationSubmissionEventHelper, AccreditationSubmissionEventHelper>()
            .AddScoped<ISubmissionEventsValidator, SubmissionEventsValidator>()
            .AddScoped<ISubmissionHydrationService, SubmissionHydrationService>()
            .AddValidatorsFromAssembly(Assembly.GetExecutingAssembly())
            .AddMediatrAndPipelines();

    private static IServiceCollection AddMessagingServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IServiceBusTopicCreator, ServiceBusTopicCreator>();
        var config = configuration.GetSection(ServiceBusOptions.ConfigSection)
            .Get<ServiceBusOptions>();

        services.AddAzureClients(builder =>
        {
            builder.AddServiceAdministrationBusClient(config.AdminConnectionString);
        });

        return services;
    }
    
    private static IServiceCollection AddMediatrAndPipelines(this IServiceCollection services)
    {
        return services
            .AddMediatR(cfg =>
            {
                cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
                cfg.AddOpenBehavior(typeof(ValidationBehaviour<,>));
                cfg.AddOpenBehavior(typeof(UnhandledExceptionBehaviour<,>));
                cfg.AddOpenBehavior(typeof(LoggingBehaviour<,>));
                cfg.AddOpenBehavior(typeof(PerformanceBehaviour<,>));
                
                // GetSubmission chain. QueryHandler -> above behaviours -> Hydrate submission
                cfg.AddBehavior<IPipelineBehavior<SubmissionGetQuery, ErrorOr<AbstractSubmissionGetResponse>>, HydrateSubmissionBehaviour>();
                
                // GetSubmissions chain. QueryHandler -> above behaviours -> Hydrate each submission
                cfg.AddBehavior<IPipelineBehavior<SubmissionsGetQuery, ErrorOr<List<AbstractSubmissionGetResponse>>>, HydrateSubmissionsBehaviour>();
            });
    }
}