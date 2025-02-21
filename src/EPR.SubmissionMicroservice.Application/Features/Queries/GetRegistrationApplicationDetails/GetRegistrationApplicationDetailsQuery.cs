using System.Diagnostics.CodeAnalysis;
using EPR.SubmissionMicroservice.Application.Features.Queries.Common;
using ErrorOr;
using MediatR;

namespace EPR.SubmissionMicroservice.Application.Features.Queries.GetRegistrationApplicationDetails;

[ExcludeFromCodeCoverage]
public class GetRegistrationApplicationDetailsQuery : IRequest<ErrorOr<GetRegistrationApplicationDetailsResponse>>
{
    public Guid OrganisationId { get; set; }

    public Guid? ComplianceSchemeId { get; set; }

    public string SubmissionPeriod { get; set; } = null!;

    public DateTime LateFeeDeadline { get; set; }
}