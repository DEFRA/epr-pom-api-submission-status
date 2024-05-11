namespace EPR.SubmissionMicroservice.Application.Features.Queries.SubmissionOrganisationDetailsGet;

using ErrorOr;
using MediatR;

public record SubmissionOrganisationDetailsGetQuery(Guid SubmissionId, string BlobName) : IRequest<ErrorOr<SubmissionOrganisationDetailsGetResponse>>;