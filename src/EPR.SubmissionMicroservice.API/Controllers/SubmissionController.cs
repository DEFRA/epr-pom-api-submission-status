using EPR.SubmissionMicroservice.Application.Features.Queries.GetRegistrationApplicationDetails;

namespace EPR.SubmissionMicroservice.API.Controllers;

using Application.Features.Commands.SubmissionCreate;
using Application.Features.Commands.SubmissionSubmit;
using Application.Features.Queries.Common;
using Application.Features.Queries.SubmissionFileGet;
using Application.Features.Queries.SubmissionGet;
using Application.Features.Queries.SubmissionOrganisationDetailsGet;
using Application.Features.Queries.SubmissionsEventsGet;
using Application.Features.Queries.SubmissionsGet;
using Application.Features.Queries.SubmissionsPeriodGet;
using Asp.Versioning;
using AutoMapper;
using Contracts.Submission.Create;
using Contracts.Submission.Submit;
using Contracts.Submissions.Get;
using EPR.SubmissionMicroservice.Application.Features.Queries.SubmissionUploadedFileGet;
using Filters.Swashbuckle;
using Filters.Swashbuckle.Examples;
using Microsoft.AspNetCore.Mvc;
using Services.Interfaces;
using Swashbuckle.AspNetCore.Filters;

[ApiVersion("1.0")]
[Route("/v{version:apiVersion}/submissions")]
[SwaggerTag]
public class SubmissionController : ApiController
{
    private readonly IMapper _mapper;
    private readonly IHeaderSetter _headerSetter;

    public SubmissionController(IMapper mapper, IHeaderSetter headerSetter)
    {
        _mapper = mapper;
        _headerSetter = headerSetter;
    }

    [HttpPost(Name = nameof(CreateSubmission))]
    [Consumes("application/json")]
    [SwaggerRequestExample(typeof(SubmissionCreateRequest), typeof(PostSubmissionExample))]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateSubmission(SubmissionCreateRequest request)
    {
        var command = _headerSetter.Set(_mapper.Map<SubmissionCreateCommand>(request));

        var result = await Mediator.Send(command);

        return result.IsError
            ? Problem(result.Errors)
            : new CreatedAtRouteResult(
                nameof(GetSubmission),
                new { submissionId = result.Value.Id },
                null);
    }

    [HttpGet("{submissionId:guid}", Name = nameof(GetSubmission))]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PomSubmissionGetResponse))]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(RegistrationSubmissionGetResponse))]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(SubsidiarySubmissionGetResponse))]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(CompaniesHouseSubmissionGetResponse))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSubmission([FromRoute] Guid submissionId, [FromHeader] Guid organisationId)
    {
        var result = await Mediator.Send(new SubmissionGetQuery(submissionId, organisationId));

        return result.IsError
            ? Problem(result.Errors)
            : Ok(result.Value);
    }

    [HttpGet(Name = nameof(GetSubmissions))]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetSubmissions([FromQuery] SubmissionsGetRequest request)
    {
        var query = _headerSetter.Set(_mapper.Map<SubmissionsGetQuery>(request));
        var result = await Mediator.Send(query);

        return result.IsError
            ? Problem(result.Errors)
            : Ok(result.Value);
    }

    [HttpGet("files/{fileId}", Name = nameof(GetSubmissionFile))]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(SubmissionFileGetResponse))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSubmissionFile([FromRoute] Guid fileId)
    {
        var result = await Mediator.Send(new SubmissionFileGetQuery(fileId));

        return result.IsError
            ? Problem(result.Errors)
            : Ok(result.Value);
    }

    [HttpGet("{submissionId}/uploadedfile/{fileId}", Name = nameof(GetSubmissionUploadedFile))]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(SubmissionUploadedFileGetResponse))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSubmissionUploadedFile([FromRoute] Guid submissionId, [FromRoute] Guid fileId)
    {
        var result = await Mediator.Send(new SubmissionUploadedFileGetQuery(fileId, submissionId));

        return result.IsError
            ? Problem(result.Errors)
            : Ok(result.Value);
    }

    [HttpGet("{submissionId:guid}/organisation-details", Name = nameof(GetSubmissionOrganisationDetails))]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(SubmissionOrganisationDetailsGetResponse))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSubmissionOrganisationDetails([FromRoute] Guid submissionId, [FromQuery] string blobName)
    {
        var result = await Mediator.Send(new SubmissionOrganisationDetailsGetQuery(submissionId, blobName));

        return result.IsError
            ? Problem(result.Errors)
            : Ok(result.Value);
    }

    [HttpPost("{submissionId:guid}/submit")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Submit([FromRoute] Guid submissionId, [FromBody] SubmissionPayload request)
    {
        var command = _mapper.Map<SubmissionSubmitCommand>(request);
        command = _headerSetter.Set(command);
        command.SubmissionId = submissionId;

        var result = await Mediator.Send(command);

        return result.IsError
            ? Problem(result.Errors)
            : NoContent();
    }

    [HttpGet("events/events-by-type/{submissionId:guid}", Name = nameof(GetSubmissionEvents))]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(SubmissionsEventsGetResponse))]
    public async Task<IActionResult> GetSubmissionEvents([FromRoute] Guid submissionId, [FromQuery] FileSubmissionsEventGetRequest request)
    {
        var query = new SubmissionsEventsGetQuery(submissionId, request.LastSyncTime);

        var result = await Mediator.Send(query);

        return result.IsError
           ? Problem(result.Errors)
           : Ok(result.Value);
    }

    [HttpGet("submissions", Name = nameof(GetSubmissionsByType))]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(SubmissionGetResponse))]
    public async Task<IActionResult> GetSubmissionsByType([FromQuery] SubmissionGetRequest request)
    {
        var query = _mapper.Map<SubmissionsPeriodGetQuery>(request);

        var result = await Mediator.Send(query);

        return result.IsError
           ? Problem(result.Errors)
           : Ok(result.Value);
    }

    [HttpGet("get-registration-application-details", Name = nameof(GetRegistrationApplicationDetails))]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(SubmissionGetResponse))]
    public async Task<IActionResult> GetRegistrationApplicationDetails([FromQuery] GetRegistrationApplicationDetailsRequest request)
    {
        var query = _mapper.Map<GetRegistrationApplicationDetailsQuery>(request);

        var result = await Mediator.Send(query);

        return result.Value is null
               ? NoContent()
               : Ok(result.Value);
    }

    [HttpGet("get-packaging-data-resubmission-application-details", Name = nameof(GetPackagingDataResubmissionApplicationDetails))]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(SubmissionGetResponse))]
    public async Task<IActionResult> GetPackagingDataResubmissionApplicationDetails([FromQuery] GetPackagingResubmissionApplicationDetailsRequest request)
    {
        var query = _mapper.Map<GetPackagingResubmissionApplicationDetailsQuery>(request);

        var result = await Mediator.Send(query);

        return result.Value is null
               ? NoContent()
               : Ok(result.Value);
    }
}