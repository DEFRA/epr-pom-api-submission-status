using AutoMapper;
using EPR.SubmissionMicroservice.Application.Features.Queries.Common;
using EPR.SubmissionMicroservice.Application.Features.Queries.Helpers.Interfaces;
using EPR.SubmissionMicroservice.Application.Options;
using EPR.SubmissionMicroservice.Data.Entities.ValidationEventError;
using EPR.SubmissionMicroservice.Data.Repositories.Queries.Interfaces;
using ErrorOr;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace EPR.SubmissionMicroservice.Application.Features.Queries.ValidationEventErrorGet;

public class ValidationEventErrorGetQueryHandler : IRequestHandler<ValidationEventErrorGetQuery, ErrorOr<List<AbstractValidationIssueGetResponse>>>
{
    private readonly ValidationOptions _validationOptions;
    private readonly IQueryRepository<AbstractValidationError> _validationEventErrorQueryRepository;
    private readonly IValidationEventHelper _validationEventHelper;
    private readonly IMapper _mapper;

    public ValidationEventErrorGetQueryHandler(
        IQueryRepository<AbstractValidationError> validationEventErrorQueryRepository,
        IValidationEventHelper validationEventHelper,
        IMapper mapper,
        IOptions<ValidationOptions> validationOptions)
    {
        _validationEventErrorQueryRepository = validationEventErrorQueryRepository;
        _validationEventHelper = validationEventHelper;
        _mapper = mapper;
        _validationOptions = validationOptions.Value;
    }

    public async Task<ErrorOr<List<AbstractValidationIssueGetResponse>>> Handle(ValidationEventErrorGetQuery request, CancellationToken cancellationToken)
    {
        var errors = new List<AbstractValidationError>();
        var latestAntivirusResultEvent = await _validationEventHelper.GetLatestAntivirusResult(request.SubmissionId, cancellationToken);

        if (latestAntivirusResultEvent != null)
        {
            var validationErrors = await _validationEventErrorQueryRepository
                .GetAll(x => x.BlobName == latestAntivirusResultEvent.BlobName)
                .OrderBy(x => x.RowNumber)
                .Take(_validationOptions.MaxIssuesToProcess)
                .ToListAsync(cancellationToken);

            errors.AddRange(validationErrors);
        }

        return _mapper.Map<List<AbstractValidationIssueGetResponse>>(errors);
    }
}