namespace EPR.SubmissionMicroservice.API.Controllers;

using Application.Features.Commands.SubmissionCreate;
using Application.Features.Commands.SubmissionSubmit;
using Application.Features.Queries.Common;
using Application.Features.Queries.SubmissionFileGet;
using Application.Features.Queries.SubmissionGet;
using Application.Features.Queries.SubmissionsGet;
using AutoMapper;
using Contracts.Submission.Create;
using Contracts.Submission.Submit;
using Contracts.Submissions.Get;
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

    [HttpPost("{submissionId:guid}/submit")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Submit(
        [FromRoute] Guid submissionId,
        [FromBody] SubmissionPayload request)
    {
        var command = _mapper.Map<SubmissionSubmitCommand>(request);
        command = _headerSetter.Set(command);
        command.SubmissionId = submissionId;

        var result = await Mediator.Send(command);

        return result.IsError
            ? Problem(result.Errors)
            : NoContent();
    }
}