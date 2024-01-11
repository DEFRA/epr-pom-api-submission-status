﻿namespace EPR.SubmissionMicroservice.API.Contracts.Decisions.Get;

public class DecisionGetRequest
{
    public DateTime LastSyncTime { get; set; } = DateTime.Parse("01 January 2000");

    public int? Limit { get; set; }

    public Guid SubmissionId { get; set; }
}