using Asp.Versioning;
using EPR.SubmissionMicroservice.API.Filters.Swashbuckle;
using EPR.SubmissionMicroservice.Application.Features.Queries.Common;
using EPR.SubmissionMicroservice.Application.Features.Queries.RegistrationValidationErrors;
using EPR.SubmissionMicroservice.Application.Features.Queries.ValidationEventErrorGet;
using Microsoft.AspNetCore.Mvc;

namespace EPR.SubmissionMicroservice.API.Controllers;

[ApiVersion("1.0")]
[Route("/v{version:apiVersion}/submissions")]
[SwaggerTag]
public class ValidationEventErrorController : ApiController
{
    [HttpGet("{submissionId:guid}/producer-validations", Name = nameof(Get))]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ProducerValidationIssueGetResponse))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(Guid submissionId)
    {
        var result = await Mediator.Send(new ValidationEventErrorGetQuery(submissionId));

        return result.IsError
            ? Problem(result.Errors)
            : Ok(result.Value);
    }

    [HttpGet("{submissionId:guid}/organisation-details-errors", Name = nameof(GetOrganisationDetailsErrors))]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(RegistrationValidationIssueGetResponse))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetOrganisationDetailsErrors(
        [FromRoute] Guid submissionId,
        [FromHeader] Guid organisationId)
    {
        var result = await Mediator.Send(new RegistrationValidationErrorQuery(submissionId, organisationId));

        return result.IsError
            ? Problem(result.Errors)
            : Ok(result.Value);
    }
}