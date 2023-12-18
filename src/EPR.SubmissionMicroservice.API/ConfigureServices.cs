namespace EPR.SubmissionMicroservice.API;

using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Common.Functions.AccessControl.Interfaces;
using Common.Functions.CancellationTokens.Interfaces;
using Common.Logging.Extensions;
using Errors;
using Filters.Swashbuckle;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.OpenApi.Models;
using MockServices;
using Services;
using Services.Interfaces;
using Swashbuckle.AspNetCore.Filters;
using IUserContextProvider = Application.Interfaces.IUserContextProvider;

[ExcludeFromCodeCoverage]
public static class ConfigureServices
{
    public static IServiceCollection AddApiServices(this IServiceCollection services, IConfiguration configuration) =>
        services
            .RegisterBaseServices()
            .RegisterCommonServices();

    private static IServiceCollection RegisterCommonServices(this IServiceCollection services) =>
        services
            .AddAutoMapper(Assembly.GetExecutingAssembly())
            .AddScoped<Common.Functions.AccessControl.Interfaces.IUserContextProvider, MockUserContextProvider>()
            .AddScoped<IUserContextProvider, UserContextProvider>()
            .AddScoped<IAuthenticator, MockAuthenticator>()
            .AddScoped<ICancellationTokenAccessor, MockCancellationTokenAccessor>()
            .AddScoped<IHeaderSetter, HeaderSetter>()
            .AddSingleton<IHeaderParser, HeaderParser>();

    private static IServiceCollection RegisterBaseServices(this IServiceCollection services)
    {
        services.AddAutoMapper(Assembly.GetExecutingAssembly());
        services.AddApiVersioning();
        services.ConfigureLogging();

        services
            .AddControllers()
            .AddNewtonsoftJson();

        services.AddTransient<ProblemDetailsFactory, CustomProblemDetailsFactory>();

        services.AddHealthChecks();

        services
            .AddEndpointsApiExplorer()
            .AddSwaggerGen(c =>
            {
                c.SwaggerDoc(
                    "v1",
                    new OpenApiInfo
                    {
                        Version = "v1",
                        Title = "EPR Submission API",
                        Description = "API for submitting PoM and Registration Data to EPR",
                    });
                c.ExampleFilters();
                c.OperationFilter<HeaderFilter>();
                c.DocumentFilter<SwaggerFilterOutControllers>();
            })
            .AddSwaggerExamplesFromAssemblies(Assembly.GetEntryAssembly());

        return services;
    }
}