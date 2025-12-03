using System.Diagnostics.CodeAnalysis;

namespace EPR.SubmissionMicroservice.API.Contracts.Submission.Submit;

[ExcludeFromCodeCoverage]
public class SubmissionPayload
{
    public string? SubmittedBy { get; set; }

    public Guid FileId { get; set; }

    public string? AppReferenceNumber { get; set; }

    public bool? IsResubmission { get; set; }

    public string? RegistrationJourney { get; set; }
}