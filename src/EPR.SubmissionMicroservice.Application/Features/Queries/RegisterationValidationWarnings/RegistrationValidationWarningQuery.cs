using EPR.SubmissionMicroservice.Application.Features.Queries.Common;
using ErrorOr;
using MediatR;

namespace EPR.SubmissionMicroservice.Application.Features.Queries.RegisterationValidationWarnings;

public record RegistrationValidationWarningQuery(Guid SubmissionId, Guid OrganisationId) : IRequest<ErrorOr<List<AbstractValidationIssueGetResponse>>>;
