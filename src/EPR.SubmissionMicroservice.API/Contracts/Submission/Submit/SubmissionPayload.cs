namespace EPR.SubmissionMicroservice.API.Contracts.Submission.Submit;

public class SubmissionPayload
{
    public string? SubmittedBy { get; set; }

    public Guid FileId { get; set; }

    public string? AppReferenceNumber { get; set; }
}