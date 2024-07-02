using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using EPR.SubmissionMicroservice.Application.Behaviours;
using EPR.SubmissionMicroservice.Application.Features.Commands.SubmissionSubmit;
using EPR.SubmissionMicroservice.Application.Features.Queries.Helpers;
using EPR.SubmissionMicroservice.Application.Features.Queries.Helpers.Interfaces;
using EPR.SubmissionMicroservice.Application.Options;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

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
    }

    private static IServiceCollection AddBaseServices(this IServiceCollection services) =>
        services
            .AddAutoMapper(Assembly.GetExecutingAssembly())
            .AddScoped<IPomSubmissionEventHelper, PomSubmissionEventHelper>()
            .AddScoped<IValidationEventHelper, ValidationEventHelper>()
            .AddScoped<IRegistrationSubmissionEventHelper, RegistrationSubmissionEventHelper>()
            .AddScoped<ISubmissionEventsValidator, SubmissionEventsValidator>()
            .AddValidatorsFromAssembly(Assembly.GetExecutingAssembly())
            .AddMediatrAndPipelines();

    private static IServiceCollection AddMediatrAndPipelines(this IServiceCollection services) =>
        services
            .AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()))
            .AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehaviour<,>))
            .AddTransient(typeof(IPipelineBehavior<,>), typeof(UnhandledExceptionBehaviour<,>))
            .AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehaviour<,>))
            .AddTransient(typeof(IPipelineBehavior<,>), typeof(PerformanceBehaviour<,>));
}