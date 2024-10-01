﻿using EPR.SubmissionMicroservice.Application.Features.Queries.Common;
using EPR.SubmissionMicroservice.Application.Features.Queries.Helpers.Interfaces;
using EPR.SubmissionMicroservice.Data.Entities.AntivirusEvents;
using EPR.SubmissionMicroservice.Data.Entities.SubmissionEvent;
using EPR.SubmissionMicroservice.Data.Enums;
using EPR.SubmissionMicroservice.Data.Repositories.Queries.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EPR.SubmissionMicroservice.Application.Features.Queries.Helpers;

public class CompaniesHouseSubmissionEventHelper : ICompaniesHouseSubmissionEventHelper
{
    private readonly IQueryRepository<AbstractSubmissionEvent> _submissionEventQueryRepository;

    public CompaniesHouseSubmissionEventHelper(
        IQueryRepository<AbstractSubmissionEvent> submissionEventQueryRepository)
    {
        _submissionEventQueryRepository = submissionEventQueryRepository;
    }

    public async Task SetValidationEventsAsync(CompaniesHouseSubmissionGetResponse response, CancellationToken cancellationToken)
    {
        var submissionId = response.Id;
        var fileUploadErrors = new List<string>();

        var antivirusCheckEvent = await _submissionEventQueryRepository
            .GetAll(x => x.SubmissionId == submissionId && x.Type == EventType.AntivirusCheck)
            .OrderByDescending(x => x.Created)
            .Cast<AntivirusCheckEvent>()
            .FirstOrDefaultAsync(cancellationToken);

        if (antivirusCheckEvent is null)
        {
            return;
        }

        if (antivirusCheckEvent.Errors.Count > 0)
        {
            fileUploadErrors.AddRange(antivirusCheckEvent.Errors);
        }

        var antivirusResultEvent = await _submissionEventQueryRepository
            .GetAll(x => x.Type == EventType.AntivirusResult)
            .Cast<AntivirusResultEvent>()
            .Where(x => x.FileId == antivirusCheckEvent.FileId)
            .FirstOrDefaultAsync(cancellationToken);

        var processingComplete = false;

        if (antivirusResultEvent is not null
            && antivirusCheckEvent.Errors.Count == 0
            && antivirusResultEvent.Errors.Count == 0
            && antivirusResultEvent.AntivirusScanResult == AntivirusScanResult.Success)
        {
            processingComplete = true;
        }

        if (antivirusResultEvent is not null && antivirusResultEvent.Errors.Count > 0)
        {
            fileUploadErrors.AddRange(antivirusResultEvent.Errors);
        }

        response.CompaniesHouseFileName = antivirusCheckEvent.FileName;
        response.CompaniesHouseFileUploadDateTime = antivirusCheckEvent.Created;
        response.CompaniesHouseDataComplete = antivirusCheckEvent.Errors.Count < 1;
        response.ValidationPass = true;
        response.Errors = fileUploadErrors;
        response.HasWarnings = false;
        response.CompaniesHouseDataComplete = processingComplete;
    }
}