namespace EPR.SubmissionMicroservice.API.UnitTests.Mapping;

using API.Contracts.Submissions.Get;
using API.Mapping;
using Application.Features.Commands.SubmissionCreate;
using Application.Features.Queries.SubmissionsGet;
using AutoMapper;
using Data.Enums;
using FluentAssertions;
using TestSupport;

[TestClass]
public class SubmissionProfileTests
{
    private IMapper _mapper;

    [TestInitialize]
    public async Task Setup()
    {
        MapperConfiguration mapperConfig = new MapperConfiguration(
            cfg =>
            {
                cfg.AddProfile(new SubmissionProfile());
            });

        _mapper = new Mapper(mapperConfig);
    }

    [TestMethod]
    [DataRow(SubmissionType.Producer)]
    [DataRow(SubmissionType.Registration)]
    public async Task SubmissionCreateRequest_MapsTo_SubmissionCreateCommand(SubmissionType submissionType)
    {
        // Arrange
        var request = TestRequests.Submission.ValidSubmissionCreateRequest(submissionType);

        // Act
        var result = _mapper.Map<SubmissionCreateCommand>(request);

        // Assert
        result.Should().NotBeNull();
    }

    [TestMethod]
    public async Task SubmissionsGetRequest_MapsTo_SubmissionsGetQuery()
    {
        // Arrange
        var request = new SubmissionsGetRequest();

        // Act
        var result = _mapper.Map<SubmissionsGetQuery>(request);

        // Assert
        result.Should().NotBeNull();
    }
}