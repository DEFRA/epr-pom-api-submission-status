﻿using System.Diagnostics.CodeAnalysis;
using EPR.SubmissionMicroservice.Data.Enums;

namespace EPR.SubmissionMicroservice.Application.Features.Queries.Common;

[ExcludeFromCodeCoverage]
public abstract class AbstractSubmissionEventGetResponse
{
    public Guid SubmissionId { get; set; }

    public virtual EventType Type { get; set; }

    public List<string> Errors { get; set; }

    public Guid? UserId { get; set; }

    public string? BlobName { get; set; }

    public string? BlobContainerName { get; set; }

    public DateTime Created { get; set; }

    public string Comments { get; set; } = string.Empty;

    public RegulatorDecision Decision { get; set; } = RegulatorDecision.None;

    public bool IsResubmissionRequired { get; set; } = false;
}