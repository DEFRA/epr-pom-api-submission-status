using EPR.SubmissionMicroservice.Application.Features.Queries.SubmissionEventsGet;

namespace EPR.SubmissionMicroservice.API.Services;

using Application.Features.Commands.SubmissionCreate;
using Application.Features.Commands.SubmissionEventCreate;
using Application.Features.Commands.SubmissionSubmit;
using Application.Features.Queries.SubmissionsGet;
using Application.Interfaces;
using Interfaces;
using MediatR;

public class HeaderSetter : IHeaderSetter
{
    private readonly IUserContextProvider _userContextProvider;

    public HeaderSetter(IUserContextProvider userContextProvider)
    {
        _userContextProvider = userContextProvider;
    }

    public T Set<T>(T command)
        where T : IRequest => command;

    public SubmissionCreateCommand Set(SubmissionCreateCommand command)
    {
        command.OrganisationId = _userContextProvider.OrganisationId;
        command.UserId = _userContextProvider.UserId;
        return command;
    }

    public SubmissionSubmitCommand Set(SubmissionSubmitCommand command)
    {
        command.UserId = _userContextProvider.UserId;
        return command;
    }

    public AbstractSubmissionEventCreateCommand Set(AbstractSubmissionEventCreateCommand command)
    {
        command.UserId = _userContextProvider.UserId;
        return command;
    }

    public RegulatorPoMDecisionSubmissionEventsGetQuery Set(RegulatorPoMDecisionSubmissionEventsGetQuery command)
    {
        return command;
    }

    public RegulatorRegistrationDecisionSubmissionEventsGetQuery Set(RegulatorRegistrationDecisionSubmissionEventsGetQuery command)
    {
        return command;
    }

    public SubmissionsGetQuery Set(SubmissionsGetQuery command)
    {
        command.OrganisationId = _userContextProvider.OrganisationId;
        return command;
    }

    public RegulatorPoMDecisionSubmissionEventGetQuery Set(RegulatorPoMDecisionSubmissionEventGetQuery command)
    {
        return command;
    }
}