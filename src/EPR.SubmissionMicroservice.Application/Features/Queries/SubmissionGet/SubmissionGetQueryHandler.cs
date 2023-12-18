namespace EPR.SubmissionMicroservice.Application.Features.Queries.SubmissionGet;

using AutoMapper;
using Common;
using Data.Constants;
using Data.Entities.Submission;
using Data.Enums;
using Data.Repositories.Queries.Interfaces;
using EPR.Common.Functions.Exceptions;
using ErrorOr;
using Helpers.Interfaces;
using MediatR;

public class SubmissionGetQueryHandler : IRequestHandler<SubmissionGetQuery, ErrorOr<AbstractSubmissionGetResponse>>
{
    private readonly IQueryRepository<Submission> _submissionQueryRepository;
    private readonly IPomSubmissionEventHelper _pomSubmissionEventHelper;
    private readonly IRegistrationSubmissionEventHelper _registrationSubmissionEventHelper;
    private readonly IMapper _mapper;

    public SubmissionGetQueryHandler(
        IQueryRepository<Submission> submissionQueryRepository,
        IPomSubmissionEventHelper pomSubmissionEventHelper,
        IRegistrationSubmissionEventHelper registrationSubmissionEventHelper,
        IMapper mapper)
    {
        _submissionQueryRepository = submissionQueryRepository;
        _mapper = mapper;
        _pomSubmissionEventHelper = pomSubmissionEventHelper;
        _registrationSubmissionEventHelper = registrationSubmissionEventHelper;
    }

    public async Task<ErrorOr<AbstractSubmissionGetResponse>> Handle(
        SubmissionGetQuery request,
        CancellationToken cancellationToken)
    {
        var submission = await _submissionQueryRepository.GetByIdAsync(request.Id, cancellationToken);

        if (submission is null)
        {
            return Error.NotFound();
        }

        if (submission.OrganisationId != request.OrganisationId)
        {
            return Error.Custom(CustomErrorType.Unauthorized, CustomErrorCode.OrganisationUnauthorized, "Your organisation is not authorized to access the submission.");
        }

        switch (submission.SubmissionType)
        {
            case SubmissionType.Producer:
                var pomResponse = _mapper.Map<PomSubmissionGetResponse>(submission);
                await _pomSubmissionEventHelper.SetValidationEventsAsync(pomResponse, submission is { IsSubmitted: true }, cancellationToken);
                return pomResponse;

            case SubmissionType.Registration:
                var registrationResponse = _mapper.Map<RegistrationSubmissionGetResponse>(submission);
                await _registrationSubmissionEventHelper.SetValidationEvents(registrationResponse, submission is { IsSubmitted: true }, cancellationToken);
                return registrationResponse;
            default:
                throw new BadRequestException("Undefined submission type");
        }
    }
}