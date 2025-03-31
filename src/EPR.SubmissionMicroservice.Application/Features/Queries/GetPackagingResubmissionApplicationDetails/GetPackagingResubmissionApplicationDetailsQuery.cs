using System.Diagnostics.CodeAnalysis;
using EPR.SubmissionMicroservice.Application.Features.Queries.Common;
using ErrorOr;
using MediatR;

namespace EPR.SubmissionMicroservice.Application.Features.Queries.GetRegistrationApplicationDetails;

[ExcludeFromCodeCoverage]
public class GetPackagingResubmissionApplicationDetailsQuery : IRequest<ErrorOr<List<GetPackagingResubmissionApplicationDetailsResponse>>>
{
    public Guid OrganisationId { get; set; }

    public Guid? ComplianceSchemeId { get; set; }

    public List<string> SubmissionPeriods { get; set; } = new List<string>()!;
}