namespace EPR.SubmissionMicroservice.Application.Options;

public record ServiceBusOptions
{
    public const string ConfigSection = "ServiceBus";
    public string ConnectionString { get; init; }
    public string AdminConnectionString { get; init; }
    public string RegistrationSubmittedForFeesCalculationTopicName { get; init; }
}