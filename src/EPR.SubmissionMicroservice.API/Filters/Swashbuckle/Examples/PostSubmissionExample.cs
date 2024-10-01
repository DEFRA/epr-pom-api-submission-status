namespace EPR.SubmissionMicroservice.API.Filters.Swashbuckle.Examples;

using System.Diagnostics.CodeAnalysis;
using Contracts.Submission.Create;
using Data.Enums;
using global::Swashbuckle.AspNetCore.Filters;

[ExcludeFromCodeCoverage]
public class PostSubmissionExample : IMultipleExamplesProvider<SubmissionCreateRequest>
{
    public IEnumerable<SwaggerExample<SubmissionCreateRequest>> GetExamples()
    {
        yield return SwaggerExample.Create(
            "PoM",
            new SubmissionCreateRequest
            {
                Id = Guid.NewGuid(),
                SubmissionType = SubmissionType.Producer,
                DataSourceType = DataSourceType.File,
                SubmissionPeriod = "2023",
            });

        yield return SwaggerExample.Create(
            "Registration",
            new SubmissionCreateRequest
            {
                Id = Guid.NewGuid(),
                SubmissionType = SubmissionType.Registration,
                DataSourceType = DataSourceType.File,
                SubmissionPeriod = "2023",
            });

        yield return SwaggerExample.Create(
            "Subsidiary",
            new SubmissionCreateRequest
            {
                Id = Guid.NewGuid(),
                SubmissionType = SubmissionType.Subsidiary,
                DataSourceType = DataSourceType.File,
                SubmissionPeriod = "NA Subsidiary File Upload",
            });

        yield return SwaggerExample.Create(
            "CompaniesHouse",
            new SubmissionCreateRequest
            {
                Id = Guid.NewGuid(),
                SubmissionType = SubmissionType.CompaniesHouse,
                DataSourceType = DataSourceType.File,
                SubmissionPeriod = "NA Companies House File Upload",
            });
    }
}