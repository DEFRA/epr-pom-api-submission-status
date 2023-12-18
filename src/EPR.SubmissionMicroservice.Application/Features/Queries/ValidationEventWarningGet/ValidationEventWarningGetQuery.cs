using EPR.SubmissionMicroservice.Application.Features.Queries.Common;
using ErrorOr;
using MediatR;

namespace EPR.SubmissionMicroservice.Application.Features.Queries.ValidationEventWarningGet;

public record ValidationEventWarningGetQuery(Guid SubmissionId) : IRequest<ErrorOr<List<AbstractValidationIssueGetResponse>>>;