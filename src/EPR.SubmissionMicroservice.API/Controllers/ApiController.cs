namespace EPR.SubmissionMicroservice.API.Controllers;

using Application;
using Data.Constants;
using ErrorOr;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

[ApiController]
public class ApiController : ControllerBase
{
    private ISender? _mediator;

    protected ISender Mediator => _mediator ??= HttpContext.RequestServices.GetRequiredService<ISender>();

    protected ActionResult Problem(List<Error> errors)
    {
        if (errors.All(x => x.Type == ErrorType.Validation))
        {
            var modelStateDictionary = new ModelStateDictionary();

            foreach (var error in errors)
            {
                modelStateDictionary.AddModelError(error.Code, error.Description);
            }

            return ValidationProblem(modelStateDictionary);
        }

        HttpContext.Items[Constants.Http.Errors] = errors;

        var firstError = errors.First();

        var statusCode = firstError switch
        {
            { NumericType: CustomErrorType.Unauthorized } => StatusCodes.Status401Unauthorized,
            { Type: ErrorType.NotFound } => StatusCodes.Status404NotFound,
            { Type: ErrorType.Conflict } => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status500InternalServerError
        };

        return Problem(statusCode: statusCode, detail: firstError.Type.ToString());
    }
}