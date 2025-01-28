namespace EPR.SubmissionMicroservice.Application.Features.Queries.SubmissionUploadedFileGet;

using System.Diagnostics.CodeAnalysis;
using ErrorOr;
using MediatR;

[ExcludeFromCodeCoverage]
public record SubmissionUploadedFileGetQuery(Guid FileId, Guid SubmissionId) : IRequest<ErrorOr<SubmissionUploadedFileGetResponse>>;