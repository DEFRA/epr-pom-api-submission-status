using EPR.SubmissionMicroservice.Application.Features.Queries.Common;
using ErrorOr;
using MediatR;

namespace EPR.SubmissionMicroservice.Application.Features.Queries.SubmissionEventsGet;

public class RegulatorRegistrationDecisionSubmissionEventsGetQuery : IRequest<ErrorOr<List<RegulatorRegistrationDecisionGetResponse>>>
{
    public DateTime LastSyncTime { get; set; }
}