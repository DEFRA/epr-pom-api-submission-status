namespace EPR.SubmissionMicroservice.API.Services;

using Application.Models;
using Interfaces;

public class HeaderParser : IHeaderParser
{
    public PomHeader? Parse(IHeaderDictionary header)
    {
        return new PomHeader(
            Guid.TryParse(header[nameof(PomHeader.OrganisationId)], out var organisationId) ? organisationId : Guid.Empty,
            Guid.TryParse(header[nameof(PomHeader.UserId)], out var userId) ? userId : Guid.Empty);
    }
}