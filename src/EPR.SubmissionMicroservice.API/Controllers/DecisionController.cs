namespace EPR.SubmissionMicroservice.API.Controllers;

using AutoMapper;
using Contracts.Decisions.Get;
using EPR.SubmissionMicroservice.Application.Features.Queries.SubmissionEventsGet;
using Filters.Swashbuckle;
using Microsoft.AspNetCore.Mvc;
using Services.Interfaces;

[ApiVersion("1.0")]
[Route("/v{version:apiVersion}/decisions")]
[SwaggerTag]
public class DecisionController : ApiController
{
    private readonly IMapper _mapper;
    private readonly IHeaderSetter _headerSetter;

    public DecisionController(IMapper mapper, IHeaderSetter headerSetter)
    {
        _mapper = mapper;
        _headerSetter = headerSetter;
    }

    [HttpGet(Name = nameof(GetDecisions))]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetDecisions([FromQuery] DecisionGetRequest request)
    {
        var query = _headerSetter.Set(_mapper.Map<RegulatorPoMDecisionSubmissionEventsGetQuery>(request));
        var result = await Mediator.Send(query);

        return result.IsError
            ? Problem(result.Errors)
            : Ok(result.Value);
    }
}