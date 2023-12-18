namespace EPR.SubmissionMicroservice.API.Services;

using Application.Interfaces;

public class UserContextProvider : IUserContextProvider
{
    public Guid UserId { get; set; }

    public Guid OrganisationId { get; set; }

    public string EmailAddress { get; set; }

    public void SetContext(Guid userId, Guid organisationId)
    {
        UserId = userId;
        OrganisationId = organisationId;
    }
}