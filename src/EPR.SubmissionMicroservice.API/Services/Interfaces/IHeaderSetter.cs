using EPR.SubmissionMicroservice.Application.Features.Queries.SubmissionEventsGet;

namespace EPR.SubmissionMicroservice.API.Services.Interfaces;

using Application.Features.Commands.SubmissionCreate;
using Application.Features.Commands.SubmissionEventCreate;
using Application.Features.Commands.SubmissionSubmit;
using Application.Features.Queries.SubmissionsGet;
using MediatR;

public interface IHeaderSetter
{
    T Set<T>(T command)
        where T : IRequest;

    SubmissionCreateCommand Set(SubmissionCreateCommand command);

    SubmissionsGetQuery Set(SubmissionsGetQuery command);

    SubmissionSubmitCommand Set(SubmissionSubmitCommand command);

    AbstractSubmissionEventCreateCommand Set(AbstractSubmissionEventCreateCommand command);

    RegulatorPoMDecisionSubmissionEventsGetQuery Set(RegulatorPoMDecisionSubmissionEventsGetQuery command);
}