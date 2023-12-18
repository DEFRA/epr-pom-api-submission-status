namespace EPR.SubmissionMicroservice.Application.Features.Queries.SubmissionFileGet;

using ErrorOr;
using MediatR;

public record SubmissionFileGetQuery(Guid FileId) : IRequest<ErrorOr<SubmissionFileGetResponse>>;