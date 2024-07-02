using EPR.SubmissionMicroservice.Application.Features.Queries.Common;
using EPR.SubmissionMicroservice.Data.Enums;
using ErrorOr;
using MediatR;

namespace EPR.SubmissionMicroservice.Application.Features.Queries.SubmissionEventsGet;

public class RegulatorDecisionSubmissionEventGetQuery : IRequest<ErrorOr<List<AbstractSubmissionEventGetResponse>>>
{
    public Guid FileId { get; set; }

    public string Decision { get; set; } = string.Empty;

    public string Comments { get; set; } = string.Empty;

    public bool IsResubmissionRequired { get; set; }

    public DateTime LastSyncTime { get; set; }

    public Guid SubmissionId { get; set; }

    public SubmissionType Type { get; set; }
}
