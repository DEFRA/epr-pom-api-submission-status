namespace EPR.SubmissionMicroservice.Application.UnitTests.Features.Queries.SubmissionsPeriodGet;

using System.Collections.Generic;
using Application.Features.Queries.SubmissionsPeriodGet;
using Data.Entities.Submission;
using Data.Repositories.Queries.Interfaces;
using EPR.SubmissionMicroservice.Data.Enums;

[TestClass]
public class SubmissionsPeriodGetQueryHandlerTests
{
    private readonly Guid _complianceSchemeId = Guid.NewGuid();
    private readonly Guid _organisationId = Guid.NewGuid();
    private Mock<IQueryRepository<Submission>> _submissionQueryRepositoryMock;
    private SubmissionsPeriodGetQueryHandler _systemUnderTest;

    [TestInitialize]
    public void TestInitialize()
    {
        _submissionQueryRepositoryMock = new Mock<IQueryRepository<Submission>>();
        _systemUnderTest = new SubmissionsPeriodGetQueryHandler(
            _submissionQueryRepositoryMock.Object,
            AutoMapperHelpers.GetMapper());
    }

    [TestMethod]
    public async Task Handle_ReturnsExpectedGetResponses()
    {
        // Arrange
        var query = new SubmissionsPeriodGetQuery
        {
            OrganisationId = _organisationId,
            Year = 2023,
            Type = SubmissionType.Producer,
            ComplianceSchemeId = _complianceSchemeId,
            RegistrationJourney = "EXPECTED_REG_JOURNEY"
        };

        var submissions = new List<Submission>
        {
            new()
            {
                Id = Guid.NewGuid(),
                OrganisationId = _organisationId,
                SubmissionType = SubmissionType.Producer,
                Created = DateTime.Parse("2023-01-01"),
                ComplianceSchemeId = _complianceSchemeId,
                RegistrationJourney = "EXPECTED_REG_JOURNEY"
            }
        };

        _submissionQueryRepositoryMock
            .Setup(x => x.GetAll(It.IsAny<Expression<Func<Submission, bool>>>()))
            .Returns(submissions.BuildMock());

        // Act
        var result = await _systemUnderTest.Handle(query, CancellationToken.None);

        // Assert
        result.Value.Should().HaveCount(1);
        result.IsError.Should().BeFalse();
        result.Value[0].SubmissionId.Should().Be(submissions[0].Id);
        result.Value[0].RegistrationJourney.Should().Be(submissions[0].RegistrationJourney);

        _submissionQueryRepositoryMock
            .Verify(
                x => x.GetAll(x => x.OrganisationId == query.OrganisationId &&
                                       x.SubmissionType == query.Type &&
                                       x.Created != null),
                Times.Once);
    }

    [TestMethod]
    public async Task Handle_ReturnsExpectedEmptyGetResponses_FilteredByRegistrationJourney()
    {
        // Arrange
        var query = new SubmissionsPeriodGetQuery
        {
            OrganisationId = _organisationId,
            Year = 2023,
            Type = SubmissionType.Producer,
            ComplianceSchemeId = _complianceSchemeId,
            RegistrationJourney = "REG_JOURNEY"
        };

        var submissions = new List<Submission>
        {
            new()
            {
                Id = Guid.NewGuid(),
                OrganisationId = _organisationId,
                SubmissionType = SubmissionType.Producer,
                Created = DateTime.Parse("2023-01-01"),
                ComplianceSchemeId = _complianceSchemeId,
                RegistrationJourney = "NOT_MATCHING_REG_JOURNEY"
            }
        };

        _submissionQueryRepositoryMock
            .Setup(x => x.GetAll(It.IsAny<Expression<Func<Submission, bool>>>()))
            .Returns(submissions.BuildMock());

        // Act
        var result = await _systemUnderTest.Handle(query, CancellationToken.None);

        // Assert
        result.Value.Should().HaveCount(0);
        result.IsError.Should().BeFalse();
        _submissionQueryRepositoryMock
            .Verify(
                x => x.GetAll(x => x.OrganisationId == query.OrganisationId &&
                                   x.SubmissionType == query.Type &&
                                   x.Created != null),
                Times.Once);
    }

    [TestMethod]
    public async Task Handle_ReturnsExpectedGetResponses_WhenYearIsNull()
    {
        // Arrange
        var query = new SubmissionsPeriodGetQuery
        {
            OrganisationId = _organisationId,
            Type = SubmissionType.Producer
        };

        var submissions = new List<Submission>
        {
            new()
            {
                Id = Guid.NewGuid(),
                OrganisationId = _organisationId,
                SubmissionType = SubmissionType.Producer,
                Created = new DateTime(2023, 1, 1),
                ComplianceSchemeId = _complianceSchemeId
            }
        };

        _submissionQueryRepositoryMock
            .Setup(x => x.GetAll(It.IsAny<Expression<Func<Submission, bool>>>()))
            .Returns(submissions.BuildMock());

        // Act
        var result = await _systemUnderTest.Handle(query, CancellationToken.None);

        // Assert
        result.Value.Should().HaveCount(1);
        result.IsError.Should().BeFalse();
        result.Value[0].SubmissionId.Should().Be(submissions[0].Id);

        _submissionQueryRepositoryMock
            .Verify(x => x.GetAll(x => x.OrganisationId == query.OrganisationId && x.SubmissionType == query.Type && x.Created != null), Times.Once);
    }

    [TestMethod]
    public async Task Handle_ReturnsError_WhenInvalidYearIsProvided()
    {
        // Arrange
        var query = new SubmissionsPeriodGetQuery
        {
            Year = 9999
        };

        var submissions = new List<Submission>();

        _submissionQueryRepositoryMock
            .Setup(x => x.GetAll(It.IsAny<Expression<Func<Submission, bool>>>()))
            .Returns(submissions.BuildMock());

        // Act
        var result = await _systemUnderTest.Handle(query, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
    }
}
