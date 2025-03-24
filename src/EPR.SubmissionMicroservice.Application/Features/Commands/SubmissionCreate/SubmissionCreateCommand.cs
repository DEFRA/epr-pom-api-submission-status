namespace EPR.SubmissionMicroservice.Application.Features.Commands.SubmissionCreate;

using Data.Enums;
using ErrorOr;
using MediatR;

public class SubmissionCreateCommand : IRequest<ErrorOr<SubmissionCreateResponse>>
{
    public Guid Id { get; set; }

    public SubmissionType SubmissionType { get; set; }

    public string SubmissionPeriod { get; set; }

    public DataSourceType DataSourceType { get; set; }

    public Guid? OrganisationId { get; set; }

    public Guid? UserId { get; set; }

    public Guid? ComplianceSchemeId { get; set; }

    public bool? IsResubmission { get; set; }
}