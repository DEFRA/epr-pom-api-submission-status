using EPR.SubmissionMicroservice.Application.Features.Commands.SubmissionCreate;
using EPR.SubmissionMicroservice.Application.Features.Commands.SubmissionEventCreate;
using EPR.SubmissionMicroservice.Application.Features.Commands.SubmissionSubmit;
using EPR.SubmissionMicroservice.Application.Features.Queries.SubmissionEventsGet;
using EPR.SubmissionMicroservice.Application.Features.Queries.SubmissionsGet;
using MediatR;

namespace EPR.SubmissionMicroservice.API.Services.Interfaces;

public interface IHeaderSetter
{
    T Set<T>(T command)
        where T : IRequest;

    SubmissionCreateCommand Set(SubmissionCreateCommand command);

    SubmissionsGetQuery Set(SubmissionsGetQuery command);

    SubmissionSubmitCommand Set(SubmissionSubmitCommand command);

    AbstractSubmissionEventCreateCommand Set(AbstractSubmissionEventCreateCommand command);

    RegulatorPoMDecisionSubmissionEventsGetQuery Set(RegulatorPoMDecisionSubmissionEventsGetQuery command);

    RegulatorRegistrationDecisionSubmissionEventsGetQuery Set(RegulatorRegistrationDecisionSubmissionEventsGetQuery command);

    RegulatorOrganisationRegistrationDecisionSubmissionEventsGetQuery Set(RegulatorOrganisationRegistrationDecisionSubmissionEventsGetQuery command);

    RegulatorDecisionSubmissionEventGetQuery Set(RegulatorDecisionSubmissionEventGetQuery command);
}