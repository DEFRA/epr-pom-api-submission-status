namespace EPR.SubmissionMicroservice.Application.UnitTests.Features.Queries.SubmissionsGet;

using System.Linq.Expressions;
using Application.Features.Queries.Common;
using Application.Features.Queries.Helpers.Interfaces;
using Application.Features.Queries.SubmissionsGet;
using Data.Entities.Submission;
using Data.Enums;
using Data.Repositories.Queries.Interfaces;
using FluentAssertions;
using MockQueryable.Moq;
using Moq;
using TestSupport.Helpers;

[TestClass]
public class SubmissionsGetQueryHandlerTests
{
    private readonly Guid _complianceSchemeId = Guid.NewGuid();
    private readonly Guid _organisationId = Guid.NewGuid();
    private Mock<IQueryRepository<Submission>> _submissionQueryRepositoryMock;
    private Mock<IPomSubmissionEventHelper> _pomSubmissionEventHelperMock;
    private Mock<IRegistrationSubmissionEventHelper> _registrationSubmissionEventHelperMock;
    private SubmissionsGetQueryHandler _systemUnderTest;

    [TestInitialize]
    public void TestInitialize()
    {
        _submissionQueryRepositoryMock = new Mock<IQueryRepository<Submission>>();
        _pomSubmissionEventHelperMock = new Mock<IPomSubmissionEventHelper>();
        _registrationSubmissionEventHelperMock = new Mock<IRegistrationSubmissionEventHelper>();
        _systemUnderTest = new SubmissionsGetQueryHandler(
            _submissionQueryRepositoryMock.Object,
            _pomSubmissionEventHelperMock.Object,
            _registrationSubmissionEventHelperMock.Object,
            AutoMapperHelpers.GetMapper());
    }

    [TestMethod]
    public async Task Handle_ReturnsExpectedGetResponses()
    {
        // Arrange
        var query = new SubmissionsGetQuery
        {
            OrganisationId = _organisationId
        };

        var submissions = new List<Submission>
        {
            new()
            {
                Id = Guid.NewGuid(),
                OrganisationId = _organisationId,
                SubmissionType = SubmissionType.Registration
            },
            new()
            {
                Id = Guid.NewGuid(),
                OrganisationId = _organisationId,
                SubmissionType = SubmissionType.Producer
            }
        };

        _submissionQueryRepositoryMock
            .Setup(x => x.GetAll(It.IsAny<Expression<Func<Submission, bool>>>()))
            .Returns(submissions.BuildMock);

        // Act
        var result = await _systemUnderTest.Handle(query, CancellationToken.None);

        // Assert
        result.Value.Should().HaveCount(2);
        result.Value[0].Id.Should().Be(submissions[0].Id);
        result.Value[1].Id.Should().Be(submissions[1].Id);

        _submissionQueryRepositoryMock.Verify(x => x.GetAll(e => e.OrganisationId == query.OrganisationId), Times.Once);
        _pomSubmissionEventHelperMock.Verify(x => x.SetValidationEventsAsync(It.IsAny<PomSubmissionGetResponse>(), false, CancellationToken.None), Times.Once);
        _registrationSubmissionEventHelperMock.Verify(x => x.SetValidationEvents(It.IsAny<RegistrationSubmissionGetResponse>(), It.IsAny<bool>(), CancellationToken.None), Times.Once);
    }

    [TestMethod]
    public async Task Handle_ThrowsArgumentException_WhenSubmissionTypeIsUnknown()
    {
        // Arrange
        var query = new SubmissionsGetQuery
        {
            OrganisationId = _organisationId
        };

        var submissions = new List<Submission>
        {
            new()
            {
                Id = Guid.NewGuid(),
                OrganisationId = _organisationId
            }
        };

        _submissionQueryRepositoryMock
            .Setup(x => x.GetAll(It.IsAny<Expression<Func<Submission, bool>>>()))
            .Returns<Expression<Func<Submission, bool>>>(expr => submissions.Where(expr.Compile()).BuildMock());

        // Act / Assert
        await _systemUnderTest.Invoking(x => x.Handle(query, CancellationToken.None))
            .Should()
            .ThrowAsync<ArgumentException>()
            .WithMessage("Unknown submissionType");

        _submissionQueryRepositoryMock.Verify(x => x.GetAll(e => e.OrganisationId == query.OrganisationId), Times.Once);
        _pomSubmissionEventHelperMock.Verify(x => x.SetValidationEventsAsync(It.IsAny<PomSubmissionGetResponse>(), It.IsAny<bool>(), CancellationToken.None), Times.Never);
        _registrationSubmissionEventHelperMock.Verify(x => x.SetValidationEvents(It.IsAny<RegistrationSubmissionGetResponse>(), It.IsAny<bool>(), CancellationToken.None), Times.Never);
    }

    [TestMethod]
    public async Task Handle_ReturnsExpectedSubmissionList_WhenSubmissionsExistForTheProvidedOrganisationId()
    {
        // Arrange
        var submissions = new List<Submission>
        {
            new()
            {
                Id = Guid.NewGuid(),
                SubmissionType = SubmissionType.Registration,
                OrganisationId = _organisationId
            }
        };

        var query = new SubmissionsGetQuery
        {
            OrganisationId = submissions[0].OrganisationId
        };

        _submissionQueryRepositoryMock
            .Setup(x => x.GetAll(It.IsAny<Expression<Func<Submission, bool>>>()))
            .Returns<Expression<Func<Submission, bool>>>(expr => submissions.Where(expr.Compile()).BuildMock());

        // Act
        var result = await _systemUnderTest.Handle(query, CancellationToken.None);

        // Assert
        result.Value.Should().HaveCount(1);
        result.Value[0].Id.Should().Be(submissions[0].Id);

        _submissionQueryRepositoryMock.Verify(x => x.GetAll(e => e.OrganisationId == query.OrganisationId), Times.Once);
    }

    [TestMethod]
    public async Task Handle_ReturnsExpectedSubmissionList_WhenSubmissionsDoNotExistForTheProvidedOrganisationId()
    {
        // Arrange
        var submissions = new List<Submission>
        {
            new()
            {
                Id = Guid.NewGuid(),
                SubmissionType = SubmissionType.Registration,
                OrganisationId = _organisationId
            }
        };

        var query = new SubmissionsGetQuery
        {
            OrganisationId = Guid.NewGuid()
        };

        _submissionQueryRepositoryMock
            .Setup(x => x.GetAll(It.IsAny<Expression<Func<Submission, bool>>>()))
            .Returns<Expression<Func<Submission, bool>>>(expr => submissions.Where(expr.Compile()).BuildMock());

        // Act
        var result = await _systemUnderTest.Handle(query, CancellationToken.None);

        // Assert
        result.Value.Should().BeEmpty();

        _submissionQueryRepositoryMock.Verify(x => x.GetAll(e => e.OrganisationId == query.OrganisationId), Times.Once);
    }

    [TestMethod]
    public async Task Handle_ReturnsExpectedSubmissionList_WhenSubmissionsExistForTheProvidedSubmissionPeriods()
    {
        // Arrange
        var submissions = new List<Submission>
        {
            new()
            {
                Id = Guid.NewGuid(),
                OrganisationId = _organisationId,
                SubmissionType = SubmissionType.Registration,
                SubmissionPeriod = "Submission Period 1"
            },
            new()
            {
                Id = Guid.NewGuid(),
                OrganisationId = _organisationId,
                SubmissionType = SubmissionType.Registration,
                SubmissionPeriod = "Submission Period 2"
            },
            new()
            {
                Id = Guid.NewGuid(),
                OrganisationId = _organisationId,
                SubmissionType = SubmissionType.Registration,
                SubmissionPeriod = "Submission Period 3"
            }
        };

        var query = new SubmissionsGetQuery
        {
            OrganisationId = _organisationId,
            Periods = new List<string>
            {
                submissions[0].SubmissionPeriod,
                submissions[1].SubmissionPeriod
            }
        };

        _submissionQueryRepositoryMock
            .Setup(x => x.GetAll(It.IsAny<Expression<Func<Submission, bool>>>()))
            .Returns<Expression<Func<Submission, bool>>>(expr => submissions.Where(expr.Compile()).BuildMock());

        // Act
        var result = await _systemUnderTest.Handle(query, CancellationToken.None);

        // Assert
        result.Value.Should().HaveCount(2);
        result.Value[0].Id.Should().Be(submissions[0].Id);
        result.Value[0].SubmissionPeriod.Should().Be(submissions[0].SubmissionPeriod);
        result.Value[1].Id.Should().Be(submissions[1].Id);
        result.Value[1].SubmissionPeriod.Should().Be(submissions[1].SubmissionPeriod);

        _submissionQueryRepositoryMock.Verify(x => x.GetAll(e => e.OrganisationId == query.OrganisationId), Times.Once);
    }

    [TestMethod]
    public async Task Handle_ReturnsExpectedSubmissionList_WhenThereAreNoSubmissionPeriodsProvided()
    {
        // Arrange
        var submissions = new List<Submission>
        {
            new()
            {
                Id = Guid.NewGuid(),
                OrganisationId = _organisationId,
                SubmissionType = SubmissionType.Registration,
                SubmissionPeriod = "Submission Period 1"
            },
            new()
            {
                Id = Guid.NewGuid(),
                OrganisationId = _organisationId,
                SubmissionType = SubmissionType.Registration,
                SubmissionPeriod = "Submission Period 2"
            },
            new()
            {
                Id = Guid.NewGuid(),
                OrganisationId = _organisationId,
                SubmissionType = SubmissionType.Registration,
                SubmissionPeriod = "Submission Period 3"
            }
        };

        var query = new SubmissionsGetQuery
        {
            OrganisationId = _organisationId,
            Periods = new List<string>()
        };

        _submissionQueryRepositoryMock
            .Setup(x => x.GetAll(It.IsAny<Expression<Func<Submission, bool>>>()))
            .Returns<Expression<Func<Submission, bool>>>(expr => submissions.Where(expr.Compile()).BuildMock());

        // Act
        var result = await _systemUnderTest.Handle(query, CancellationToken.None);

        // Assert
        result.Value.Should().HaveCount(3);
        result.Value[0].Id.Should().Be(submissions[0].Id);
        result.Value[0].SubmissionPeriod.Should().Be(submissions[0].SubmissionPeriod);
        result.Value[1].Id.Should().Be(submissions[1].Id);
        result.Value[1].SubmissionPeriod.Should().Be(submissions[1].SubmissionPeriod);
        result.Value[2].Id.Should().Be(submissions[2].Id);
        result.Value[2].SubmissionPeriod.Should().Be(submissions[2].SubmissionPeriod);

        _submissionQueryRepositoryMock.Verify(x => x.GetAll(e => e.OrganisationId == query.OrganisationId), Times.Once);
    }

    [TestMethod]
    public async Task Handle_ReturnsExpectedSubmissionList_WhenSubmissionsDoNotExistForTheProvidedSubmissionPeriods()
    {
        // Arrange
        var submissions = new List<Submission>
        {
            new()
            {
                Id = Guid.NewGuid(),
                OrganisationId = _organisationId,
                SubmissionType = SubmissionType.Registration,
                SubmissionPeriod = "Submission Period 1"
            },
            new()
            {
                Id = Guid.NewGuid(),
                OrganisationId = _organisationId,
                SubmissionType = SubmissionType.Registration,
                SubmissionPeriod = "Submission Period 2"
            },
            new()
            {
                Id = Guid.NewGuid(),
                OrganisationId = _organisationId,
                SubmissionType = SubmissionType.Registration,
                SubmissionPeriod = "Submission Period 3"
            }
        };

        var query = new SubmissionsGetQuery
        {
            OrganisationId = _organisationId,
            Periods = new List<string>
            {
                "Submission Period 4"
            }
        };

        _submissionQueryRepositoryMock
            .Setup(x => x.GetAll(It.IsAny<Expression<Func<Submission, bool>>>()))
            .Returns<Expression<Func<Submission, bool>>>(expr => submissions.Where(expr.Compile()).BuildMock());

        // Act
        var result = await _systemUnderTest.Handle(query, CancellationToken.None);

        // Assert
        result.Value.Should().BeEmpty();

        _submissionQueryRepositoryMock.Verify(x => x.GetAll(e => e.OrganisationId == query.OrganisationId), Times.Once);
    }

    [TestMethod]
    public async Task Handle_ReturnsExpectedSubmissionList_WhenSubmissionsExistForTheProvidedComplianceSchemeId()
    {
        // Arrange
        var submissions = new List<Submission>
        {
            new()
            {
                Id = Guid.NewGuid(),
                OrganisationId = _organisationId,
                SubmissionType = SubmissionType.Registration,
                ComplianceSchemeId = Guid.NewGuid()
            }
        };

        var query = new SubmissionsGetQuery
        {
            OrganisationId = submissions[0].OrganisationId,
            ComplianceSchemeId = submissions[0].ComplianceSchemeId
        };

        _submissionQueryRepositoryMock
            .Setup(x => x.GetAll(It.IsAny<Expression<Func<Submission, bool>>>()))
            .Returns<Expression<Func<Submission, bool>>>(expr => submissions.Where(expr.Compile()).BuildMock());

        // Act
        var result = await _systemUnderTest.Handle(query, CancellationToken.None);

        // Assert
        result.Value.Should().HaveCount(1);
        result.Value[0].Id.Should().Be(submissions[0].Id);
        result.Value[0].ComplianceSchemeId.Should().Be(submissions[0].ComplianceSchemeId);

        _submissionQueryRepositoryMock.Verify(x => x.GetAll(e => e.OrganisationId == query.OrganisationId), Times.Once);
    }

    [TestMethod]
    public async Task Handle_ReturnsExpectedSubmissionList_WhenSubmissionsDoNotExistForTheProvidedComplianceSchemeId()
    {
        // Arrange
        var submissions = new List<Submission>
        {
            new()
            {
                Id = Guid.NewGuid(),
                OrganisationId = _organisationId,
                SubmissionType = SubmissionType.Registration,
                ComplianceSchemeId = Guid.NewGuid()
            }
        };

        var query = new SubmissionsGetQuery
        {
            OrganisationId = submissions[0].OrganisationId,
            ComplianceSchemeId = Guid.NewGuid()
        };

        _submissionQueryRepositoryMock
            .Setup(x => x.GetAll(It.IsAny<Expression<Func<Submission, bool>>>()))
            .Returns<Expression<Func<Submission, bool>>>(expr => submissions.Where(expr.Compile()).BuildMock());

        // Act
        var result = await _systemUnderTest.Handle(query, CancellationToken.None);

        // Assert
        result.Value.Should().BeEmpty();

        _submissionQueryRepositoryMock.Verify(x => x.GetAll(e => e.OrganisationId == query.OrganisationId), Times.Once);
    }

    [TestMethod]
    public async Task Handle_ReturnsExpectedSubmissionList_WhenThereIsNoComplianceSchemeIdProvided()
    {
        // Arrange
        var submissions = new List<Submission>
        {
            new()
            {
                Id = Guid.NewGuid(),
                OrganisationId = _organisationId,
                SubmissionType = SubmissionType.Registration,
                ComplianceSchemeId = Guid.NewGuid()
            }
        };

        var query = new SubmissionsGetQuery
        {
            OrganisationId = submissions[0].OrganisationId
        };

        _submissionQueryRepositoryMock
            .Setup(x => x.GetAll(It.IsAny<Expression<Func<Submission, bool>>>()))
            .Returns<Expression<Func<Submission, bool>>>(expr => submissions.Where(expr.Compile()).BuildMock());

        // Act
        var result = await _systemUnderTest.Handle(query, CancellationToken.None);

        // Assert
        result.Value.Should().HaveCount(1);
        result.Value[0].Id.Should().Be(submissions[0].Id);
        result.Value[0].ComplianceSchemeId.Should().Be(submissions[0].ComplianceSchemeId);

        _submissionQueryRepositoryMock.Verify(x => x.GetAll(e => e.OrganisationId == query.OrganisationId), Times.Once);
    }

    [TestMethod]
    public async Task Handle_ReturnsExpectedSubmissionList_WhenSubmissionsExistForTheProvidedSubmissionType()
    {
        // Arrange
        var submissions = new List<Submission>
        {
            new()
            {
                Id = Guid.NewGuid(),
                OrganisationId = _organisationId,
                SubmissionType = SubmissionType.Registration
            }
        };

        var query = new SubmissionsGetQuery
        {
            OrganisationId = submissions[0].OrganisationId,
            Type = SubmissionType.Registration
        };

        _submissionQueryRepositoryMock
            .Setup(x => x.GetAll(It.IsAny<Expression<Func<Submission, bool>>>()))
            .Returns<Expression<Func<Submission, bool>>>(expr => submissions.Where(expr.Compile()).BuildMock());

        // Act
        var result = await _systemUnderTest.Handle(query, CancellationToken.None);

        // Assert
        result.Value.Should().HaveCount(1);
        result.Value[0].Id.Should().Be(submissions[0].Id);
        result.Value[0].SubmissionType.Should().Be(submissions[0].SubmissionType);

        _submissionQueryRepositoryMock.Verify(x => x.GetAll(e => e.OrganisationId == query.OrganisationId), Times.Once);
    }

    [TestMethod]
    public async Task Handle_ReturnsExpectedSubmissionList_WhenSubmissionsDoNotExistForTheProvidedSubmissionType()
    {
        // Arrange
        var submissions = new List<Submission>
        {
            new()
            {
                Id = Guid.NewGuid(),
                OrganisationId = _organisationId,
                SubmissionType = SubmissionType.Registration,
            }
        };

        var query = new SubmissionsGetQuery
        {
            OrganisationId = submissions[0].OrganisationId,
            Type = SubmissionType.Producer
        };

        _submissionQueryRepositoryMock
            .Setup(x => x.GetAll(It.IsAny<Expression<Func<Submission, bool>>>()))
            .Returns<Expression<Func<Submission, bool>>>(expr => submissions.Where(expr.Compile()).BuildMock());

        // Act
        var result = await _systemUnderTest.Handle(query, CancellationToken.None);

        // Assert
        result.Value.Should().BeEmpty();

        _submissionQueryRepositoryMock.Verify(x => x.GetAll(e => e.OrganisationId == query.OrganisationId), Times.Once);
    }

    [TestMethod]
    public async Task Handle_ReturnsExpectedSubmissionList_WhenNoSubmissionTypeIsProvided()
    {
        // Arrange
        var submissions = new List<Submission>
        {
            new()
            {
                Id = Guid.NewGuid(),
                OrganisationId = _organisationId,
                SubmissionType = SubmissionType.Registration,
            },
            new()
            {
                Id = Guid.NewGuid(),
                OrganisationId = _organisationId,
                SubmissionType = SubmissionType.Producer,
            }
        };

        var query = new SubmissionsGetQuery
        {
            OrganisationId = _organisationId
        };

        _submissionQueryRepositoryMock
            .Setup(x => x.GetAll(It.IsAny<Expression<Func<Submission, bool>>>()))
            .Returns<Expression<Func<Submission, bool>>>(expr => submissions.Where(expr.Compile()).BuildMock());

        // Act
        var result = await _systemUnderTest.Handle(query, CancellationToken.None);

        // Assert
        result.Value.Should().HaveCount(2);
        result.Value[0].Id.Should().Be(submissions[0].Id);
        result.Value[0].SubmissionType.Should().Be(submissions[0].SubmissionType);
        result.Value[1].Id.Should().Be(submissions[1].Id);
        result.Value[1].SubmissionType.Should().Be(submissions[1].SubmissionType);

        _submissionQueryRepositoryMock.Verify(x => x.GetAll(e => e.OrganisationId == query.OrganisationId), Times.Once);
    }

    [TestMethod]
    public async Task Handle_ReturnsExpectedSubmissionList_WhenLimitIsGreaterThanZero()
    {
        // Arrange
        var submissions = new List<Submission>
        {
            new()
            {
                Id = Guid.NewGuid(),
                OrganisationId = _organisationId,
                SubmissionType = SubmissionType.Registration,
            },
            new()
            {
                Id = Guid.NewGuid(),
                OrganisationId = _organisationId,
                SubmissionType = SubmissionType.Producer,
            }
        };

        var query = new SubmissionsGetQuery
        {
            OrganisationId = _organisationId,
            Limit = 1
        };

        _submissionQueryRepositoryMock
            .Setup(x => x.GetAll(It.IsAny<Expression<Func<Submission, bool>>>()))
            .Returns<Expression<Func<Submission, bool>>>(expr => submissions.Where(expr.Compile()).BuildMock());

        // Act
        var result = await _systemUnderTest.Handle(query, CancellationToken.None);

        // Assert
        result.Value.Should().HaveCount(1);
        result.Value[0].Id.Should().Be(submissions[0].Id);

        _submissionQueryRepositoryMock.Verify(x => x.GetAll(e => e.OrganisationId == query.OrganisationId), Times.Once);
    }

    [TestMethod]
    [DataRow(0)]
    [DataRow(-1)]
    [DataRow(null)]
    public async Task Handle_ReturnsExpectedSubmissionList_WhenLimitIsNotGreaterThanZero(int? limit)
    {
        // Arrange
        var submissions = new List<Submission>
        {
            new()
            {
                Id = Guid.NewGuid(),
                OrganisationId = _organisationId,
                SubmissionType = SubmissionType.Registration,
            },
            new()
            {
                Id = Guid.NewGuid(),
                OrganisationId = _organisationId,
                SubmissionType = SubmissionType.Producer,
            }
        };

        var query = new SubmissionsGetQuery
        {
            OrganisationId = _organisationId,
            Limit = limit
        };

        _submissionQueryRepositoryMock
            .Setup(x => x.GetAll(It.IsAny<Expression<Func<Submission, bool>>>()))
            .Returns<Expression<Func<Submission, bool>>>(expr => submissions.Where(expr.Compile()).BuildMock());

        // Act
        var result = await _systemUnderTest.Handle(query, CancellationToken.None);

        // Assert
        result.Value.Should().HaveCount(2);
        result.Value[0].Id.Should().Be(submissions[0].Id);
        result.Value[1].Id.Should().Be(submissions[1].Id);

        _submissionQueryRepositoryMock.Verify(x => x.GetAll(e => e.OrganisationId == query.OrganisationId), Times.Once);
    }

    [TestMethod]
    public async Task Handle_ReturnsExpectedSubmissionList_WhenSubmissionExistWithComplianceSchemeId()
    {
        // Arrange
        var submissions = new List<Submission>
        {
            new()
            {
                Id = Guid.NewGuid(),
                OrganisationId = _organisationId,
                SubmissionType = SubmissionType.Registration,
                ComplianceSchemeId = _complianceSchemeId
            }
        };

        var query = new SubmissionsGetQuery
        {
            OrganisationId = _organisationId,
            ComplianceSchemeId = _complianceSchemeId,
        };

        _submissionQueryRepositoryMock
            .Setup(x => x.GetAll(It.IsAny<Expression<Func<Submission, bool>>>()))
            .Returns<Expression<Func<Submission, bool>>>(expr => submissions.Where(expr.Compile()).BuildMock());

        // Act
        var result = await _systemUnderTest.Handle(query, CancellationToken.None);

        // Assert
        result.Value.Should().HaveCount(1);
        result.Value[0].Id.Should().Be(submissions[0].Id);

        _submissionQueryRepositoryMock.Verify(x => x.GetAll(e => e.OrganisationId == query.OrganisationId), Times.Once);
    }
}