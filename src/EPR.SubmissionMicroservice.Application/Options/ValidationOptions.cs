namespace EPR.SubmissionMicroservice.Application.Options;

public class ValidationOptions
{
    public const string ConfigSection = "Validation";

    public int MaxIssuesToProcess { get; set; }
}