using EPR.SubmissionMicroservice.Application.Features.Queries.Common;
using ErrorOr;
using MediatR;

namespace EPR.SubmissionMicroservice.Application.Features.Queries.SubmissionEventsGet;

public class RegulatorPoMDecisionSubmissionEventGetQuery : IRequest<ErrorOr<List<AbstractSubmissionEventGetResponse>>>
{
    public Guid FileId { get; set; }

    public string Decision { get; set; } = string.Empty;

    public string Comments { get; set; } = string.Empty;

    public bool IsResubmissionRequired { get; set; }

    public DateTime LastSyncTime { get; set; }

    public Guid SubmissionId { get; set; }
}