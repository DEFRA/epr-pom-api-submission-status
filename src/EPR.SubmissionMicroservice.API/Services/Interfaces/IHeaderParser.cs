namespace EPR.SubmissionMicroservice.API.Services.Interfaces;

using Application.Models;

public interface IHeaderParser
{
    PomHeader? Parse(IHeaderDictionary header);
}