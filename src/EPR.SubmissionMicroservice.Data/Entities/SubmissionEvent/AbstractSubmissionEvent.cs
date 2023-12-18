namespace EPR.SubmissionMicroservice.Data.Entities.SubmissionEvent;

using System.Diagnostics.CodeAnalysis;
using Common.Functions.Database.Entities.Interfaces;
using Enums;

[ExcludeFromCodeCoverage]
public abstract class AbstractSubmissionEvent : EntityWithId, ICreated
{
    public Guid SubmissionId { get; set; }

    public virtual EventType Type { get; }

    public List<string> Errors { get; set; } = new();

    public Guid UserId { get; set; }

    public DateTime Created { get; set; }

    public string? BlobName { get; set; }

    public string? BlobContainerName { get; set; }
}
