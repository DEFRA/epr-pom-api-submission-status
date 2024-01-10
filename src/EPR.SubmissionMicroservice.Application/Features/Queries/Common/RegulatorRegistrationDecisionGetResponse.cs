using System.Diagnostics.CodeAnalysis;

namespace EPR.SubmissionMicroservice.Application.Features.Queries.Common;

[ExcludeFromCodeCoverage]
public class RegulatorRegistrationDecisionGetResponse : AbstractSubmissionEventGetResponse
{
    public Guid FileId { get; set; }

    public string Comments { get; set; }

    public string Decision { get; set; }
}