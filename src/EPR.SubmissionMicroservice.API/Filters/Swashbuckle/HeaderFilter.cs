namespace EPR.SubmissionMicroservice.API.Filters.Swashbuckle;

using System.Diagnostics.CodeAnalysis;
using global::Swashbuckle.AspNetCore.SwaggerGen;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Models;

[ExcludeFromCodeCoverage]
public class HeaderFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        operation.Parameters ??= new List<OpenApiParameter>();

        operation.Parameters.Add(new OpenApiParameter
        {
            Name = nameof(Headers.OrganisationId),
            In = ParameterLocation.Header,
            Description = "Organisation Id",
            Required = true,
            Schema = new OpenApiSchema
            {
                Type = "string",
                Format = "uuid",
                Example = new OpenApiString(Guid.NewGuid().ToString())
            }
        });

        operation.Parameters.Add(new OpenApiParameter
        {
            Name = nameof(Headers.UserId),
            In = ParameterLocation.Header,
            Description = "User Id",
            Required = true,
            Schema = new OpenApiSchema
            {
                Type = "string",
                Format = "uuid",
                Example = new OpenApiString(Guid.NewGuid().ToString())
            }
        });
    }
}