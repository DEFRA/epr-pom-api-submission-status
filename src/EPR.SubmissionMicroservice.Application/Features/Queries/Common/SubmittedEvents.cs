namespace EPR.SubmissionMicroservice.Application.Features.Queries.Common;

using System.Diagnostics.CodeAnalysis;

[ExcludeFromCodeCoverage]
public class SubmittedEvents : BaseEvent
{
    public string SubmittedBy { get; set; }

    public string? RegistrationJourney { get; set; }
}