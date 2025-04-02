using AutoMapper;
using EPR.SubmissionMicroservice.Application.Features.Queries.Common;
using EPR.SubmissionMicroservice.Application.Features.Queries.Helpers.Interfaces;
using EPR.SubmissionMicroservice.Application.Options;
using EPR.SubmissionMicroservice.Data.Entities.ValidationEventWarning;
using EPR.SubmissionMicroservice.Data.Repositories.Queries.Interfaces;
using ErrorOr;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace EPR.SubmissionMicroservice.Application.Features.Queries.RegisterationValidationWarnings;

public class RegistrationValidationWarningQueryHandler : IRequestHandler<RegistrationValidationWarningQuery, ErrorOr<List<AbstractValidationIssueGetResponse>>>
{
    private readonly ValidationOptions _validationOptions;
    private readonly IQueryRepository<AbstractValidationWarning> _validationEventWarningQueryRepository;
    private readonly IValidationEventHelper _validationEventHelper;
    private readonly IMapper _mapper;

    public RegistrationValidationWarningQueryHandler(
        IQueryRepository<AbstractValidationWarning> validationEventWarningQueryRepository,
        IValidationEventHelper validationEventHelper,
        IMapper mapper,
        IOptions<ValidationOptions> validationOptions)
    {
        _validationEventWarningQueryRepository = validationEventWarningQueryRepository;
        _validationEventHelper = validationEventHelper;
        _mapper = mapper;
        _validationOptions = validationOptions.Value;
    }

    public async Task<ErrorOr<List<AbstractValidationIssueGetResponse>>> Handle(RegistrationValidationWarningQuery request, CancellationToken cancellationToken)
    {
        var warnings = new List<AbstractValidationWarning>();
        var latestAntivirusResultEvent = await _validationEventHelper.GetLatestAntivirusResult(request.SubmissionId, cancellationToken);

        if (latestAntivirusResultEvent != null)
        {
            var validationWarnings = await _validationEventWarningQueryRepository
                .GetAll(x => x.BlobName == latestAntivirusResultEvent.BlobName)
                .OrderBy(x => x.RowNumber)
                .Take(_validationOptions.MaxIssuesToProcess)
                .ToListAsync(cancellationToken);

            warnings.AddRange(validationWarnings);
        }

        return _mapper.Map<List<AbstractValidationIssueGetResponse>>(warnings);
    }
}
