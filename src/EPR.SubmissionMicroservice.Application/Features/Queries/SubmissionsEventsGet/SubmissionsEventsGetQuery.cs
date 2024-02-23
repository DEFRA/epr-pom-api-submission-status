namespace EPR.SubmissionMicroservice.Application.Features.Queries.SubmissionsEventsGet;

using System.Diagnostics.CodeAnalysis;
using EPR.SubmissionMicroservice.Application.Features.Queries.Common;
using ErrorOr;
using MediatR;

[ExcludeFromCodeCoverage]
public class SubmissionsEventsGetQuery : IRequest<ErrorOr<SubmissionsEventsGetResponse>>
{
    public SubmissionsEventsGetQuery(Guid submissionId, DateTime lastSyncTime)
    {
        SubmissionId = submissionId;
        LastSyncTime = lastSyncTime;
    }

    public Guid SubmissionId { get; init; }

    public DateTime LastSyncTime { get; init; }
}