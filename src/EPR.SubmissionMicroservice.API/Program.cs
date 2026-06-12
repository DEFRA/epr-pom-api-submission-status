using EPR.SubmissionMicroservice.API;
using EPR.SubmissionMicroservice.API.HealthChecks;
using EPR.SubmissionMicroservice.API.Middleware;
using EPR.SubmissionMicroservice.Application;
using EPR.SubmissionMicroservice.Data;
using Microsoft.FeatureManagement;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

var buildNumber = builder.Configuration.GetValue<string>("BUILD_NUMBER");
var gitSha = builder.Configuration.GetValue<string>("GIT_SHA");

builder.Logging.ClearProviders();
builder.Host.UseSerilog((context, config) =>
{
    config.ReadFrom.Configuration(context.Configuration);
    config.Enrich.WithProperty("Application", context.HostingEnvironment.ApplicationName);
    config.Enrich.WithProperty("BuildNumber", buildNumber ?? "NOT_SET");
    config.Enrich.WithProperty("GitSha", gitSha ?? "NOT_SET");
});

builder.Services
    .AddApplicationInsightsTelemetry()
    .AddHealthChecks();

builder.Services
    .AddDataServices(builder.Configuration)
    .AddApplicationServices(builder.Configuration)
    .AddApiServices(builder.Configuration)
    .AddFeatureManagement();

var app = builder.Build();

 app.UseMiddleware<ExceptionHandlingMiddleware>();
 app.UseMiddleware<ContextMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseSerilogRequestLogging();

app.MapControllers();

app.MapHealthChecks("/admin/health", HealthCheckOptionsBuilder.Build());

// retrieve the logger
var logger = app.Services.GetRequiredService<ILogger<Program>>();
await app.Services.ConfigureMessaging(logger);

app.Run();