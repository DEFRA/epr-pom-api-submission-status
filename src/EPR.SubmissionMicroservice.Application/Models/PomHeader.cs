namespace EPR.SubmissionMicroservice.Application.Models;

using System.Diagnostics.CodeAnalysis;

[ExcludeFromCodeCoverage]
public record PomHeader(
    Guid OrganisationId,
    Guid UserId);