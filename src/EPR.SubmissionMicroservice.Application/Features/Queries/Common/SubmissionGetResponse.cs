namespace EPR.SubmissionMicroservice.Application.Features.Queries.Common;

using System.Diagnostics.CodeAnalysis;

[ExcludeFromCodeCoverage]
public class SubmissionGetResponse
{
    public Guid SubmissionId { get; set; }

    public string SubmissionPeriod { get; set; }

    public int Year { get; set; }

    public string? RegistrationJourney { get; set; }
}