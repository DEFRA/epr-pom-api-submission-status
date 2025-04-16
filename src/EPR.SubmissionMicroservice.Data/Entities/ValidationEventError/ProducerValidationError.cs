namespace EPR.SubmissionMicroservice.Data.Entities.ValidationEventError;

using System.Diagnostics.CodeAnalysis;

[ExcludeFromCodeCoverage]
public class ProducerValidationError : AbstractValidationError
{
    public string? ProducerId { get; set; }

    public string? ProducerType { get; set; }

    public string? ProducerSize { get; set; }

    public string? WasteType { get; set; }

    public string? DataSubmissionPeriod { get; set; }

    public string? SubsidiaryId { get; set; }

    public string? PackagingCategory { get; set; }

    public string? MaterialType { get; set; }

    public string? MaterialSubType { get; set; }

    public string? FromHomeNation { get; set; }

    public string? ToHomeNation { get; set; }

    public string? QuantityKg { get; set; }

    public string? QuantityUnits { get; set; }

    public string? TransitionalPackagingUnits { get; set; }

    public string? RecyclabilityRating { get; set; }
}