using EPR.SubmissionMicroservice.Application.Features.Queries.Common;
using ErrorOr;
using MediatR;

namespace EPR.SubmissionMicroservice.Application.Features.Queries.RegistrationValidationErrors;

public record RegistrationValidationErrorQuery(Guid SubmissionId, Guid OrganisationId) : IRequest<ErrorOr<List<AbstractValidationIssueGetResponse>>>;