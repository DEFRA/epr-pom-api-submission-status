using EPR.SubmissionMicroservice.Application.Features.Queries.Common;
using EPR.SubmissionMicroservice.Data.Enums;
using ErrorOr;
using MediatR;

namespace EPR.SubmissionMicroservice.Application.Features.Queries.SubmissionEventsGet;

public class RegulatorPoMDecisionSubmissionEventsGetQuery : IRequest<ErrorOr<List<AbstractSubmissionEventGetResponse>>>
{
    public Guid FileId { get; set; }

    public RegulatorDecision Decision { get; set; }

    public string Comments { get; set; }

    public bool IsResubmissionRequired { get; set; }

    public DateTime LastSyncTime { get; set; }
}