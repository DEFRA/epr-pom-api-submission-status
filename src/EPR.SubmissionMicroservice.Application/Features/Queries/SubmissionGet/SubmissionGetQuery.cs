namespace EPR.SubmissionMicroservice.Application.Features.Queries.SubmissionGet;

using Common;
using ErrorOr;
using MediatR;

public record SubmissionGetQuery(Guid Id, Guid OrganisationId) : IRequest<ErrorOr<AbstractSubmissionGetResponse>>;