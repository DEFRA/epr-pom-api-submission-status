namespace EPR.SubmissionMicroservice.API.Filters.Swashbuckle;

using System.Diagnostics.CodeAnalysis;
using global::Swashbuckle.AspNetCore.SwaggerGen;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.OpenApi.Models;

[ExcludeFromCodeCoverage]
public class SwaggerFilterOutControllers : IDocumentFilter
{
    public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
    {
        foreach (var contextApiDescription in context.ApiDescriptions)
        {
            var actionDescriptor = (ControllerActionDescriptor)contextApiDescription.ActionDescriptor;

            if (actionDescriptor.ControllerTypeInfo.GetCustomAttributes(typeof(SwaggerTagAttribute), true).Length == 0 &&
                actionDescriptor.MethodInfo.GetCustomAttributes(typeof(SwaggerTagAttribute), true).Length == 0)
            {
                var key = "/" + contextApiDescription.RelativePath.TrimEnd('/');
                swaggerDoc.Paths.Remove(key);
            }
        }
    }
}