using EPR.SubmissionMicroservice.Application.Features.Queries.Common;
using EPR.SubmissionMicroservice.Data.Entities.SubmissionEvent;
using EPR.SubmissionMicroservice.Data.Enums;
using EPR.SubmissionMicroservice.Data.Repositories.Queries.Interfaces;
using ErrorOr;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EPR.SubmissionMicroservice.Application.Features.Queries.SubmissionEventsGet;

public class RegulatorOrganisationRegistrationDecisionSubmissionEventsGetQueryHandler(
    IQueryRepository<AbstractSubmissionEvent> submissionQueryRepository)
    : IRequestHandler<RegulatorOrganisationRegistrationDecisionSubmissionEventsGetQuery,
        ErrorOr<List<RegulatorOrganisationRegistrationDecisionGetResponse>>>
{
    public async Task<ErrorOr<List<RegulatorOrganisationRegistrationDecisionGetResponse>>> Handle(
        RegulatorOrganisationRegistrationDecisionSubmissionEventsGetQuery query,
        CancellationToken cancellationToken)
    {
        var submissionEventsQuery = submissionQueryRepository
                                    .GetAll(x =>
                                            ((query.SubmissionId != null && x.SubmissionId == query.SubmissionId)
                                              || query.SubmissionId == null)
                                            && (x.Type == EventType.RegulatorRegistrationDecision
                                                 || x.Type == EventType.RegistrationApplicationSubmitted)
                                            && x.Created > query.LastSyncTime);

        submissionEventsQuery = submissionEventsQuery.OrderByDescending(x => x.Created);

        var submissionEvents = await submissionEventsQuery.ToListAsync(cancellationToken);

        var regulatorOrganisationRegistrationGetResponses = new List<RegulatorOrganisationRegistrationDecisionGetResponse>();

        RegulatorOrganisationRegistrationDecisionGetResponse regulatorOrganisationRegistrationGetResponse = default;

        foreach (var submissionEvent in submissionEvents)
        {
            var regSubmissionEvent = submissionEvent as RegulatorRegistrationDecisionEvent;
            if (regSubmissionEvent is not null)
            {
                regulatorOrganisationRegistrationGetResponse = new RegulatorOrganisationRegistrationDecisionGetResponse
                {
                    AppReferenceNumber = regSubmissionEvent.AppReferenceNumber,
                    Comments = regSubmissionEvent.Comments,
                    Created = regSubmissionEvent.Created,
                    Decision = regSubmissionEvent.Decision switch
                    {
                        RegulatorDecision.Accepted => "Granted",
                        RegulatorDecision.Approved => "Granted",
                        RegulatorDecision.Rejected => "Refused",
                        _ => regSubmissionEvent.Decision.ToString()
                    },
                    DecisionDate = regSubmissionEvent.DecisionDate,
                    RegistrationReferenceNumber = regSubmissionEvent.RegistrationReferenceNumber,
                    SubmissionId = regSubmissionEvent.SubmissionId,
                    Type = regSubmissionEvent.Type.ToString(),
                    FileId = regSubmissionEvent.FileId.ToString()
                };
                regulatorOrganisationRegistrationGetResponses.Add(regulatorOrganisationRegistrationGetResponse);
            }

            var producerEvent = submissionEvent as RegistrationApplicationSubmittedEvent;

            if (producerEvent is not null)
            {
                var producerOrganisationRegistrationGetResponse = new RegulatorOrganisationRegistrationDecisionGetResponse
                {
                    AppReferenceNumber = producerEvent.ApplicationReferenceNumber,
                    Comments = producerEvent.Comments,
                    Created = producerEvent.Created,
                    SubmissionId = producerEvent.SubmissionId,
                    Type = producerEvent.Type.ToString()
                };
                regulatorOrganisationRegistrationGetResponses.Add(producerOrganisationRegistrationGetResponse);
            }
        }

        return regulatorOrganisationRegistrationGetResponses;
    }
}