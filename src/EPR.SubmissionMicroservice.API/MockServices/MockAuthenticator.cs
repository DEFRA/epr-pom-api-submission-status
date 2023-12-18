namespace EPR.SubmissionMicroservice.API.MockServices;

using System.Diagnostics.CodeAnalysis;
using Common.Functions.AccessControl.Interfaces;

[ExcludeFromCodeCoverage]
public class MockAuthenticator : IAuthenticator
{
    public Guid UserId { get; }

    public string EmailAddress { get; }

    public Guid CustomerOrganisationId { get; }

    public Guid CustomerId { get; }

    public Task<bool> AuthenticateAsync(string bearerToken)
    {
        throw new NotImplementedException();
    }
}