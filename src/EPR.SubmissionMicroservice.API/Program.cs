using EPR.SubmissionMicroservice.API;
using EPR.SubmissionMicroservice.API.HealthChecks;
using EPR.SubmissionMicroservice.API.Middleware;
using EPR.SubmissionMicroservice.Application;
using EPR.SubmissionMicroservice.Data;
using Microsoft.FeatureManagement;

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;

builder.Services
    .AddDataServices(configuration)
    .AddApplicationServices(configuration)
    .AddApiServices(configuration)
    .AddApplicationInsightsTelemetry()
    .AddLogging()
    .AddFeatureManagement();

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

app.Run();