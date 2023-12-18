namespace EPR.SubmissionMicroservice.API.UnitTests.Services;

using API.Services;
using Application.Features.Commands.SubmissionSubmit;
using Application.Features.Queries.SubmissionsGet;
using Application.Interfaces;
using Data.Enums;
using FluentAssertions;
using Moq;
using TestSupport;

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
}