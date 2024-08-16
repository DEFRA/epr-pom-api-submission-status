using AutoMapper;
using EPR.SubmissionMicroservice.API.Contracts.SubmissionEvents.Get;
using EPR.SubmissionMicroservice.API.Filters.Swashbuckle;
using EPR.SubmissionMicroservice.API.Filters.Swashbuckle.Examples;
using EPR.SubmissionMicroservice.API.Services.Interfaces;
using EPR.SubmissionMicroservice.Application.Converters;
using EPR.SubmissionMicroservice.Application.Features.Commands.SubmissionEventCreate;
using EPR.SubmissionMicroservice.Application.Features.Queries.SubmissionEventsGet;
using ErrorOr;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Swashbuckle.AspNetCore.Filters;

namespace EPR.SubmissionMicroservice.API.Controllers;

[ApiVersion("1.0")]
[Route("/v{version:apiVersion}/submissions")]
[SwaggerTag]
public class SubmissionEventController : ApiController
{
    private readonly IMapper _mapper;
    private readonly IHeaderSetter _headerSetter;

    public SubmissionEventController(IHeaderSetter headerSetter, IMapper mapper)
    {
        _headerSetter = headerSetter;
        _mapper = mapper;
    }

    [HttpPost("{submissionId}/events", Name = nameof(CreateEvent))]
    [Consumes("application/json")]
    [SwaggerRequestExample(typeof(JObject), typeof(PostSubmissionEventExample))]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateEvent(
        [FromRoute] Guid submissionId,
        [FromBody] JObject request)
    {
        var type = request.GetValue("type", StringComparison.OrdinalIgnoreCase)?.Value<int>();
        if (type is null)
        {
            return Problem(new List<Error>
            {
                Error.Validation("Type", "'Type' is required.")
            });
        }

        var serializer = new JsonSerializer();
        serializer.Converters.Add(new EventConverter());
        serializer.Converters.Add(new EventErrorConverter());
        serializer.Converters.Add(new EventWarningConverter());

        var validationErrors = request.GetValue("validationErrors", StringComparison.OrdinalIgnoreCase)?.Value<JArray>();
        if (validationErrors?.Any() is true)
        {
            foreach (var item in validationErrors)
            {
                ((JObject)item).Add(new JProperty("validationErrorType", type));
            }
        }

        var validationWarnings = request.GetValue("validationWarnings", StringComparison.OrdinalIgnoreCase)?.Value<JArray>();
        if (validationWarnings?.Any() is true)
        {
            foreach (var item in validationWarnings)
            {
                ((JObject)item).Add(new JProperty("validationWarningType", type));
            }
        }

        try
        {
            var command = request.ToObject<AbstractSubmissionEventCreateCommand>(serializer);
            command = _headerSetter.Set(command);
            command.SubmissionId = submissionId;

            var result = await Mediator.Send(command);

            if (result.IsError)
            {
                return Problem(result.Errors);
            }

            return new ObjectResult(null) { StatusCode = StatusCodes.Status201Created };
        }
        catch (NotImplementedException)
        {
            return Problem(new List<Error>
            {
                Error.Validation("Type", $"'Type' has a range of values which does not include '{type}'.")
            });
        }
    }

    [HttpGet("events/get-regulator-pom-decision", Name = nameof(GetSubmissionEventsByType))]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesDefaultResponseType]
    public async Task<IActionResult> GetSubmissionEventsByType([FromQuery] RegulatorPoMDecisionSubmissionEventsGetRequest request)
    {
        var query = _headerSetter.Set(_mapper.Map<RegulatorPoMDecisionSubmissionEventsGetQuery>(request));
        var result = await Mediator.Send(query);

        return result.IsError
            ? Problem(result.Errors)
            : Ok(result.Value);
    }

    [HttpGet("events/get-regulator-registration-decision", Name = nameof(GetRegulatorRegistrationSubmissionEvents))]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesDefaultResponseType]
    public async Task<IActionResult> GetRegulatorRegistrationSubmissionEvents([FromQuery] RegulatorRegistrationDecisionSubmissionEventsGetRequest request)
    {
        var query = _headerSetter.Set(_mapper.Map<RegulatorRegistrationDecisionSubmissionEventsGetQuery>(request));
        var result = await Mediator.Send(query);

        return result.IsError
            ? Problem(result.Errors)
            : Ok(result.Value);
    }
}