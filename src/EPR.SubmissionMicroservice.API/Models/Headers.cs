namespace EPR.SubmissionMicroservice.API.Models;

using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Mvc;

[ExcludeFromCodeCoverage]
public class Headers
{
    [FromHeader]
    public Guid OrganisationId { get; set; }

    [FromHeader]
    public Guid UserId { get; set; }
}