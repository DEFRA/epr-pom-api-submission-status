namespace EPR.SubmissionMicroservice.Application.Features.Queries.SubmissionsGet;

using Common;
using Data.Enums;
using ErrorOr;
using MediatR;

public class SubmissionsGetQuery : IRequest<ErrorOr<List<AbstractSubmissionGetResponse>>>
{
    public Guid? OrganisationId { get; set; }

    public List<string>? Periods { get; set; }

    public SubmissionType? Type { get; set; }

    public Guid? ComplianceSchemeId { get; set; }

    public bool? IsFirstComplianceScheme { get; set; }

    public int? Limit { get; set; }
}