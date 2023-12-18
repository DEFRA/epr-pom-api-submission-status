namespace EPR.SubmissionMicroservice.Application.Features.Commands.SubmissionSubmit;

using ErrorOr;
using MediatR;

public class SubmissionSubmitCommand : IRequest<ErrorOr<Unit>>
{
    public Guid SubmissionId { get; set; }

    public Guid FileId { get; set; }

    public Guid UserId { get; set; }

    public string? SubmittedBy { get; set; }
}