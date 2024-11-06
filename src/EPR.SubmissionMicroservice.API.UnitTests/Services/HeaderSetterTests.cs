using EPR.SubmissionMicroservice.API.Services;
using EPR.SubmissionMicroservice.Application.Features.Commands.SubmissionEventCreate;
using EPR.SubmissionMicroservice.Application.Features.Commands.SubmissionSubmit;
using EPR.SubmissionMicroservice.Application.Features.Queries.SubmissionEventsGet;
using EPR.SubmissionMicroservice.Application.Features.Queries.SubmissionsGet;
using EPR.SubmissionMicroservice.Application.Interfaces;
using EPR.SubmissionMicroservice.Data.Enums;
using FluentAssertions;
using Moq;
using TestSupport;

namespace EPR.SubmissionMicroservice.API.UnitTests.Services;

[TestClass]
public class HeaderSetterTests
{
    private readonly Guid _submissionId = Guid.NewGuid();
    private readonly Mock<IUserContextProvider> _userContextProviderMock;
    private readonly HeaderSetter _systemUnderTest;

    public HeaderSetterTests()
    {
        _userContextProviderMock = new Mock<IUserContextProvider>();
        _systemUnderTest = new HeaderSetter(_userContextProviderMock.Object);
    }

    [TestMethod]
    [DataRow(SubmissionType.Producer)]
    [DataRow(SubmissionType.Registration)]
    public void Set_SubmissionCreateCommand(SubmissionType submissionType)
    {
        // Arrange
        var command = TestCommands.Submission.ValidSubmissionCreateCommand(submissionType);

        _userContextProviderMock.SetupAllProperties();

        // Act
        var result = _systemUnderTest.Set(command);

        // Assert
        result.OrganisationId.Should().Be(_userContextProviderMock.Object.OrganisationId);
        result.UserId.Should().Be(_userContextProviderMock.Object.UserId);
    }

    [TestMethod]
    [DataRow(EventType.AntivirusCheck)]
    [DataRow(EventType.CheckSplitter)]
    [DataRow(EventType.ProducerValidation)]
    [DataRow(EventType.Registration)]
    [DataRow(EventType.AntivirusResult)]
    public void Set_SubmissionEventCreateCommand(EventType eventType)
    {
        // Arrange
        var command = TestCommands.SubmissionEvent.ValidSubmissionEventCreateCommand(eventType);

        _userContextProviderMock.SetupAllProperties();

        // Act
        var result = _systemUnderTest.Set(command);

        // Assert
        result.UserId.Should().Be(_userContextProviderMock.Object.UserId);
    }

    [TestMethod]
    public void Set_SubmissionsGetCommand()
    {
        // Arrange
        var command = new SubmissionsGetQuery();

        _userContextProviderMock.SetupAllProperties();

        // Act
        var result = _systemUnderTest.Set(command);

        // Assert
        result.OrganisationId.Should().Be(_userContextProviderMock.Object.OrganisationId);
    }

    [TestMethod]
    public void Set_SetsUserIdOnSubmissionSubmitCommand()
    {
        // Arrange
        _userContextProviderMock.SetupAllProperties();
        var command = new SubmissionSubmitCommand() { SubmissionId = _submissionId };

        // Act
        var result = _systemUnderTest.Set(command);

        // Assert
        result.SubmissionId.Should().Be(_submissionId);
        result.UserId.Should().Be(_userContextProviderMock.Object.UserId);
    }

    [TestMethod]
    public void Set_ReturnsSameCommandOnRegulatorRegistrationDecisionSubmissionEventsGetQueryCommand()
    {
        // Arrange
        _userContextProviderMock.SetupAllProperties();
        var command = new RegulatorRegistrationDecisionEventCreateCommand() { SubmissionId = _submissionId };

        // Act
        var result = _systemUnderTest.Set(command);

        // Assert
        result.SubmissionId.Should().Be(_submissionId);
    }

    [TestMethod]
    public void Set_RegulatorPoMDecisionSubmissionEventCommand()
    {
        // Arrange
        var command = new RegulatorDecisionSubmissionEventGetQuery();

        _userContextProviderMock.SetupAllProperties();

        // Act
        var result = _systemUnderTest.Set(command);

        // Assert
        result.Should().NotBeNull();
        result.IsResubmissionRequired.Should().BeFalse();
        result.FileId.Should().Be(Guid.Empty);
        result.SubmissionId.Should().Be(Guid.Empty);
        result.Decision.Should().Be(string.Empty);
        result.Comments.Should().Be(string.Empty);
    }

    [TestMethod]
    public void Set_RegulatorPoMDecisionSubmissionEventsCommand()
    {
        // Arrange
        var fileId = Guid.NewGuid();
        var decision = RegulatorDecision.Approved;
        var comments = "Testing";
        var isResubmissionRequired = true;
        var lastSyncTime = DateTime.UtcNow;

        var command = new RegulatorPoMDecisionSubmissionEventsGetQuery()
        {
            FileId = fileId,
            Decision = decision,
            Comments = comments,
            IsResubmissionRequired = isResubmissionRequired,
            LastSyncTime = lastSyncTime
        };

        _userContextProviderMock.SetupAllProperties();

        // Act
        var result = _systemUnderTest.Set(command);

        // Assert
        result.Should().NotBeNull();
        result.IsResubmissionRequired.Should().BeTrue();
        result.FileId.Should().Be(fileId);
        result.Decision.Should().Be(decision);
        result.Comments.Should().Be(comments);
        result.LastSyncTime.Should().Be(lastSyncTime);
    }

    [TestMethod]
    public void Set_RegulatorRegistrationDecisionSubmissionEventsGetQueryCommand()
    {
        // Arrange
        var dateLastSync = DateTime.UtcNow;
        var command = new RegulatorRegistrationDecisionSubmissionEventsGetQuery() { LastSyncTime = dateLastSync };

        _userContextProviderMock.SetupAllProperties();

        // Act
        var result = _systemUnderTest.Set(command);

        // Assert
        result.Should().NotBeNull();
        result.LastSyncTime.Should().Be(dateLastSync);
    }

    [TestMethod]
    public void Set_RegulatorOrganisationRegistrationDecisionSubmissionEventsGetQueryCommand()
    {
        // Arrange
        var dateLastSync = DateTime.UtcNow;
        var command = new RegulatorOrganisationRegistrationDecisionSubmissionEventsGetQuery() { LastSyncTime = dateLastSync };

        _userContextProviderMock.SetupAllProperties();

        // Act
        var result = _systemUnderTest.Set(command);

        // Assert
        result.Should().NotBeNull();
        result.LastSyncTime.Should().Be(dateLastSync);
    }
}