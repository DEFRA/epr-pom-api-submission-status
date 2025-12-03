namespace EPR.SubmissionMicroservice.Application.Features.Commands.SubmissionSubmit;

using ErrorOr;
using MediatR;

public class SubmissionSubmitCommand : IRequest<ErrorOr<Unit>>
{
    public Guid SubmissionId { get; set; }

    public Guid FileId { get; set; }

    public Guid UserId { get; set; }

    public string? SubmittedBy { get; set; }

    public string? AppReferenceNumber { get; set; }

    public bool? IsResubmission { get; set; }

    public string? RegistrationJourney { get; set; }
}