namespace EPR.SubmissionMicroservice.Data.Entities;

using System.Diagnostics.CodeAnalysis;

[ExcludeFromCodeCoverage]
public abstract class EntityWithId
{
    public Guid Id { get; set; } = Guid.NewGuid();
}