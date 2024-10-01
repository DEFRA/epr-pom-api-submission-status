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
    private readonly ISubsidiarySubmissionEventHelper _subsidiarySubmissionEventHelper;
    private readonly ICompaniesHouseSubmissionEventHelper _companiesHouseSubmissionEventHelper;
    private readonly IMapper _mapper;

    public SubmissionGetQueryHandler(
        IQueryRepository<Submission> submissionQueryRepository,
        IPomSubmissionEventHelper pomSubmissionEventHelper,
        IRegistrationSubmissionEventHelper registrationSubmissionEventHelper,
        ISubsidiarySubmissionEventHelper subsidiarySubmissionEventHelper,
        ICompaniesHouseSubmissionEventHelper companiesHouseSubmissionEventHelper,
        IMapper mapper)
    {
        _submissionQueryRepository = submissionQueryRepository;
        _mapper = mapper;
        _pomSubmissionEventHelper = pomSubmissionEventHelper;
        _registrationSubmissionEventHelper = registrationSubmissionEventHelper;
        _subsidiarySubmissionEventHelper = subsidiarySubmissionEventHelper;
        _companiesHouseSubmissionEventHelper = companiesHouseSubmissionEventHelper;
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

            case SubmissionType.Subsidiary:
                var subsidiaryResponse = _mapper.Map<SubsidiarySubmissionGetResponse>(submission);
                await _subsidiarySubmissionEventHelper.SetValidationEventsAsync(subsidiaryResponse, submission is { IsSubmitted: true }, cancellationToken);
                return subsidiaryResponse;

            case SubmissionType.CompaniesHouse:
                var companiesHouseResponse = _mapper.Map<CompaniesHouseSubmissionGetResponse>(submission);
                await _companiesHouseSubmissionEventHelper.SetValidationEventsAsync(companiesHouseResponse, cancellationToken);
                return companiesHouseResponse;

            default:
                throw new BadRequestException("Undefined submission type");
        }
    }
}