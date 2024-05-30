using EPR.SubmissionMicroservice.API;
using EPR.SubmissionMicroservice.API.HealthChecks;
using EPR.SubmissionMicroservice.API.Middleware;
using EPR.SubmissionMicroservice.Application;
using EPR.SubmissionMicroservice.Data;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.FeatureManagement;

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;

builder.Services
    .AddDataServices(configuration)
    .AddApplicationServices(configuration)
    .AddApiServices(configuration)
    .AddApplicationInsightsTelemetry()
    .AddFeatureManagement();

builder.Services.AddHealthChecks();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseMiddleware<ContextMiddleware>();
app.MapControllers();

app.MapHealthChecks("/admin/health", HealthCheckOptionsBuilder.Build());

if (builder.Configuration.GetValue<bool>("FeatureManagement:AllowAlertTestEndpoint"))
{
    app.MapHealthChecks("/admin/error", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
    {
        ResultStatusCodes =
    {
        [HealthStatus.Healthy] = StatusCodes.Status500InternalServerError
    }
    }).AllowAnonymous();
}

app.Run();