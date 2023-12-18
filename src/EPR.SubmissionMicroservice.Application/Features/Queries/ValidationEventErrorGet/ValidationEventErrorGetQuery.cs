using EPR.SubmissionMicroservice.Application.Features.Queries.Common;
using ErrorOr;
using MediatR;

namespace EPR.SubmissionMicroservice.Application.Features.Queries.ValidationEventErrorGet;

public record ValidationEventErrorGetQuery(Guid SubmissionId) : IRequest<ErrorOr<List<AbstractValidationIssueGetResponse>>>;