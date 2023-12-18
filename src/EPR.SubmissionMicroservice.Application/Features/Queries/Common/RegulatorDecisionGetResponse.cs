using EPR.SubmissionMicroservice.Data.Enums;

namespace EPR.SubmissionMicroservice.Application.Features.Queries.Common;

public class RegulatorDecisionGetResponse : AbstractSubmissionEventGetResponse
{
    public Guid FileId { get; set; }

    public string Comments { get; set; }

    public string Decision { get; set; }

    public bool IsResubmissionRequired { get; set; }
}