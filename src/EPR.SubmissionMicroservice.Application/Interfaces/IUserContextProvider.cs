namespace EPR.SubmissionMicroservice.Application.Interfaces;

public interface IUserContextProvider
{
    Guid UserId { get; set; }

    Guid OrganisationId { get; set; }

    string EmailAddress { get; set; }

    void SetContext(Guid userId, Guid organisationId);
}