namespace EPR.SubmissionMicroservice.API.MockServices;

using System.Diagnostics.CodeAnalysis;
using Common.Functions.AccessControl.Interfaces;

[ExcludeFromCodeCoverage]
public class MockUserContextProvider : IUserContextProvider
{
    public Guid UserId { get; }

    public Guid CustomerOrganisationId { get; }

    public Guid CustomerId { get; }

    public string EmailAddress { get; }
}