namespace EPR.SubmissionMicroservice.Application.Features.Queries.SubmissionsPeriodGet;

using System.Diagnostics.CodeAnalysis;
using EPR.SubmissionMicroservice.Application.Features.Queries.Common;
using EPR.SubmissionMicroservice.Data.Enums;
using ErrorOr;
using MediatR;

[ExcludeFromCodeCoverage]
public class SubmissionsPeriodGetQuery : IRequest<ErrorOr<List<SubmissionGetResponse>>>
{
    public Guid OrganisationId { get; set; }

    public Guid? ComplianceSchemeId { get; set; }

    public int? Year { get; set; }

    public SubmissionType Type { get; set; }
}