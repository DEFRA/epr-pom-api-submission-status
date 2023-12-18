namespace EPR.SubmissionMicroservice.Application.Features.Queries.Common;

public abstract class AbstractValidationIssueGetResponse
{
    public int RowNumber { get; set; }

    public List<string> ErrorCodes { get; set; }
}