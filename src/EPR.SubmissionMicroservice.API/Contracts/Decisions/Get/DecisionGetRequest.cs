using System.Diagnostics.CodeAnalysis;
using EPR.SubmissionMicroservice.Data.Enums;

namespace EPR.SubmissionMicroservice.API.Contracts.Decisions.Get;

[ExcludeFromCodeCoverage]
public class DecisionGetRequest
{
    public DateTime LastSyncTime { get; set; } = DateTime.Parse("01 January 2000");

    public int? Limit { get; set; }

    public Guid SubmissionId { get; set; }

    public SubmissionType Type { get; set; }
}