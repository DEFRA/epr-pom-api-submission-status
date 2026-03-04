using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using EPR.SubmissionMicroservice.Application.Behaviours;
using EPR.SubmissionMicroservice.Application.Features.Commands.SubmissionSubmit;
using EPR.SubmissionMicroservice.Application.Features.Queries.Common;
using EPR.SubmissionMicroservice.Application.Features.Queries.Helpers;
using EPR.SubmissionMicroservice.Application.Features.Queries.Helpers.Interfaces;
using EPR.SubmissionMicroservice.Application.Features.Queries.SubmissionGet;
using EPR.SubmissionMicroservice.Application.Options;
using ErrorOr;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

[assembly: InternalsVisibleTo("EPR.SubmissionMicroservice.Application.UnitTests")]
namespace EPR.SubmissionMicroservice.Application;

[ExcludeFromCodeCoverage]
public static class ConfigureServices
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
    {
        ConfigureOptions(services, configuration);
        return services.AddBaseServices();
    }

    private static void ConfigureOptions(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<ValidationOptions>(configuration.GetSection(ValidationOptions.ConfigSection));
        services.Configure<FeatureFlagOptions>(configuration.GetSection(FeatureFlagOptions.ConfigSection));
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
            .AddValidatorsFromAssembly(Assembly.GetExecutingAssembly())
            .AddMediatrAndPipelines();

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
            });
    }
}