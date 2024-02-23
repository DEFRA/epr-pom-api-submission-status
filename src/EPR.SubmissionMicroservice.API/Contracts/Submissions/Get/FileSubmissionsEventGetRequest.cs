namespace EPR.SubmissionMicroservice.API.Contracts.Submissions.Get;

using System.Diagnostics.CodeAnalysis;

[ExcludeFromCodeCoverage]
public class FileSubmissionsEventGetRequest
{
    public DateTime LastSyncTime { get; set; }
}
