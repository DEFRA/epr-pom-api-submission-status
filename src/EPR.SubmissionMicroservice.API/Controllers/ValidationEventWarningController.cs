using Asp.Versioning;
using EPR.SubmissionMicroservice.API.Filters.Swashbuckle;
using EPR.SubmissionMicroservice.Application.Features.Queries.Common;
using EPR.SubmissionMicroservice.Application.Features.Queries.RegisterationValidationWarnings;
using EPR.SubmissionMicroservice.Application.Features.Queries.ValidationEventWarningGet;
using Microsoft.AspNetCore.Mvc;

namespace EPR.SubmissionMicroservice.API.Controllers;

[ApiVersion("1.0")]
[Route("/v{version:apiVersion}/submissions")]
[SwaggerTag]
public class ValidationEventWarningController : ApiController
{
    [HttpGet]
    [Route("{submissionId:guid}/producer-warning-validations")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ProducerValidationIssueGetResponse))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(Guid submissionId)
    {
        var result = await Mediator.Send(new ValidationEventWarningGetQuery(submissionId));

        return result.IsError
            ? Problem(result.Errors)
            : Ok(result.Value);
    }

    [HttpGet]
    [Route("{submissionId:guid}/organisation-details-warnings")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(RegistrationValidationIssueGetResponse))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetOrganisationDetailsWarnings(
        [FromRoute] Guid submissionId,
        [FromHeader] Guid organisationId)
    {
        var result = await Mediator.Send(new RegistrationValidationWarningQuery(submissionId, organisationId));

        return result.IsError
            ? Problem(result.Errors)
            : Ok(result.Value);
    }
}