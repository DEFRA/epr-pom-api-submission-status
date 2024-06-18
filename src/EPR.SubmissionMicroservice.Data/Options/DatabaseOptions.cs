using System.Diagnostics.CodeAnalysis;

namespace EPR.SubmissionMicroservice.Data.Options;

[ExcludeFromCodeCoverage]
public class DatabaseOptions
{
    public const string ConfigSection = "Database";

    public string ConnectionString { get; set; }

    public string AccountKey { get; set; }

    public string Name { get; set; }

    public int MaxRetryCount { get; set; }

    public int MaxRetryDelayInMilliseconds { get; set; }
}