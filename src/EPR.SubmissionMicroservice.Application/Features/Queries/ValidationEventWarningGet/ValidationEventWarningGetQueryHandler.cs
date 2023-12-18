using AutoMapper;
using EPR.SubmissionMicroservice.Application.Features.Queries.Common;
using EPR.SubmissionMicroservice.Application.Features.Queries.Helpers.Interfaces;
using EPR.SubmissionMicroservice.Data.Entities.ValidationEventWarning;
using EPR.SubmissionMicroservice.Data.Repositories.Queries.Interfaces;
using ErrorOr;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EPR.SubmissionMicroservice.Application.Features.Queries.ValidationEventWarningGet;

public class ValidationEventWarningGetQueryHandler : IRequestHandler<ValidationEventWarningGetQuery, ErrorOr<List<AbstractValidationIssueGetResponse>>>
{
    private readonly IQueryRepository<AbstractValidationWarning> _validationEventWarningQueryRepository;
    private readonly IValidationEventHelper _validationEventHelper;
    private readonly IMapper _mapper;

    public ValidationEventWarningGetQueryHandler(
        IQueryRepository<AbstractValidationWarning> validationEventWarningQueryRepository,
        IValidationEventHelper validationEventHelper,
        IMapper mapper)
    {
        _validationEventWarningQueryRepository = validationEventWarningQueryRepository;
        _validationEventHelper = validationEventHelper;
        _mapper = mapper;
    }

    public async Task<ErrorOr<List<AbstractValidationIssueGetResponse>>> Handle(ValidationEventWarningGetQuery request, CancellationToken cancellationToken)
    {
        var errors = new List<AbstractValidationWarning>();
        var latestAntivirusResultEvent = await _validationEventHelper.GetLatestAntivirusResult(request.SubmissionId, cancellationToken);

        if (latestAntivirusResultEvent != null)
        {
            var validationErrors = await _validationEventWarningQueryRepository
                .GetAll(x => x.BlobName == latestAntivirusResultEvent.BlobName)
                .OrderBy(x => x.RowNumber)
                .ToListAsync(cancellationToken);

            errors.AddRange(validationErrors);
        }

        return _mapper.Map<List<AbstractValidationIssueGetResponse>>(errors);
    }
}