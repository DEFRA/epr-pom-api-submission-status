namespace EPR.SubmissionMicroservice.Application.Features.Commands.SubmissionEventCreate;

using Data.Enums;
using ErrorOr;
using MediatR;

public abstract class AbstractSubmissionEventCreateCommand : IRequest<ErrorOr<SubmissionEventCreateResponse>>
{
    public Guid SubmissionId { get; set; }

    public virtual EventType Type { get; set; }

    public List<string> Errors { get; set; }

    public Guid? UserId { get; set; }

    public string? BlobName { get; set; }

    public string? BlobContainerName { get; set; }
}
