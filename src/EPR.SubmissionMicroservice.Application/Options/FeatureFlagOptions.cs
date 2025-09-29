namespace EPR.SubmissionMicroservice.Application.Options;

public class FeatureFlagOptions
{
    public const string ConfigSection = "FeatureManagement";

    public bool IsQueryLateFeeEnabled { get; set; }
}