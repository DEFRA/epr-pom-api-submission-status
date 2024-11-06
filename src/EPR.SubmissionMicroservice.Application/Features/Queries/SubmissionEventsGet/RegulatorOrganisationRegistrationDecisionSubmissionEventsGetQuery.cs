using EPR.SubmissionMicroservice.Application.Features.Queries.Common;
using ErrorOr;
using MediatR;

namespace EPR.SubmissionMicroservice.Application.Features.Queries.SubmissionEventsGet;

public class RegulatorOrganisationRegistrationDecisionSubmissionEventsGetQuery : IRequest<ErrorOr<List<RegulatorOrganisationRegistrationDecisionGetResponse>>>
{
    public DateTime LastSyncTime { get; set; }

    public Guid? SubmissionId { get; set; }
}
