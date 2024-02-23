namespace EPR.SubmissionMicroservice.Application.Features.Queries.Common;

using System.Diagnostics.CodeAnalysis;

[ExcludeFromCodeCoverage]
public abstract class BaseEvent
{
    public Guid SubmissionId { get; set; }

    public DateTime Created { get; set; }

    public Guid UserId { get; set; }

    public Guid FileId { get; set; }

    public string FileName { get; set; }
}
