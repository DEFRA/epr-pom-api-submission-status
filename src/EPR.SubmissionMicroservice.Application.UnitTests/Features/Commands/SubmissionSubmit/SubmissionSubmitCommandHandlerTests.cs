namespace EPR.SubmissionMicroservice.Application.UnitTests.Features.Commands.SubmissionSubmit;

using Application.Features.Commands.SubmissionSubmit;
using Application.Features.Queries.Helpers.Interfaces;
using Common.Functions.AccessControl.Interfaces;
using Common.Functions.Database.Decorators.Interfaces;
using Common.Functions.Services.Interfaces;
using Common.Logging.Constants;
using Common.Logging.Models;
using Common.Logging.Services;
using Data;
using Data.Entities.Submission;
using Data.Entities.SubmissionEvent;
using Data.Enums;
using Data.Repositories.Commands.Interfaces;
using Data.Repositories.Queries.Interfaces;
using FluentAssertions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

[TestClass]
public class SubmissionSubmitCommandHandlerTests
{
    private readonly Guid _submissionId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _fileId = Guid.NewGuid();
    private SubmissionSubmitCommandHandler _systemUnderTest;
    private Mock<ICommandRepository<Submission>> _submissionCommandRepositoryMock;
    private Mock<ICommandRepository<AbstractSubmissionEvent>> _submissionEventCommandRepositoryMock;
    private Mock<IQueryRepository<Submission>> _submissionQueryRepositoryMock;
    private Mock<IPomSubmissionEventHelper> _pomSubmissionEventHelperMock;
    private Mock<SubmissionContext> _submissionContextMock;
    private Mock<ILoggingService> _loggingServiceMock;
    private Mock<ILogger<SubmissionSubmitCommandHandler>> _loggerMock;
    private Mock<ISubmissionEventsValidator> _submissionEventValidatorMock;

    [TestInitialize]
    public void TestInitialize()
    {
        _submissionCommandRepositoryMock = new Mock<ICommandRepository<Submission>>();
        _submissionEventCommandRepositoryMock = new Mock<ICommandRepository<AbstractSubmissionEvent>>();
        _submissionQueryRepositoryMock = new Mock<IQueryRepository<Submission>>();
        _pomSubmissionEventHelperMock = new Mock<IPomSubmissionEventHelper>();
        _loggingServiceMock = new Mock<ILoggingService>();
        _loggerMock = new Mock<ILogger<SubmissionSubmitCommandHandler>>();
        _submissionEventValidatorMock = new Mock<ISubmissionEventsValidator>();
        _submissionContextMock = new Mock<SubmissionContext>(
            new DbContextOptions<SubmissionContext>(),
            Mock.Of<IUserContextProvider>(),
            Mock.Of<IRequestTimeService>(),
            Mock.Of<List<IEntityDecorator>>());
        _systemUnderTest = new SubmissionSubmitCommandHandler(
            _submissionCommandRepositoryMock.Object,
            _loggerMock.Object,
            _submissionQueryRepositoryMock.Object,
            _submissionEventCommandRepositoryMock.Object,
            _submissionContextMock.Object,
            _pomSubmissionEventHelperMock.Object,
            _loggingServiceMock.Object,
            _submissionEventValidatorMock.Object);
    }

    [TestMethod]
    public async Task Handle_UpdatesSubmissionAndCreatesASubmittedEvent_WhenSubmissionHasNotBeenSubmittedPreviously()
    {
        // Arrange
        var command = new SubmissionSubmitCommand { SubmissionId = _submissionId, UserId = _userId, FileId = _fileId };
        var submission = new Submission { Id = _submissionId, SubmissionType = SubmissionType.Producer, IsSubmitted = false };

        _submissionQueryRepositoryMock
            .Setup(x => x.GetByIdAsync(_submissionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(submission);
        _pomSubmissionEventHelperMock
            .Setup(x => x.VerifyFileIdIsForValidFileAsync(_submissionId, _fileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _systemUnderTest.Handle(command, CancellationToken.None);

        // Async
        result.IsError.Should().BeFalse();
        _submissionCommandRepositoryMock.Verify(x => x.Update(It.Is<Submission>(s => s.IsSubmitted.Value)), Times.Once);
        _submissionContextMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _submissionEventCommandRepositoryMock.Verify(x => x.AddAsync(It.Is<SubmittedEvent>(s => s.SubmissionId == _submissionId && s.UserId == _userId && s.FileId == _fileId)), Times.Once);
        _loggerMock.VerifyLog(x => x.LogInformation("Submission with id {submissionId} submitted by user {userId}.", _submissionId, _userId));
    }

    [TestMethod]
    public async Task Handle_DoesNotCallsUpdateSubmissionAndCreatesASubmittedEvent_WhenSubmissionHasBeenSubmittedPreviously()
    {
        // Arrange
        var command = new SubmissionSubmitCommand { SubmissionId = _submissionId, UserId = _userId, FileId = _fileId };
        var submission = new Submission { Id = _submissionId, SubmissionType = SubmissionType.Producer, IsSubmitted = true };

        _submissionQueryRepositoryMock
            .Setup(x => x.GetByIdAsync(_submissionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(submission);
        _pomSubmissionEventHelperMock
            .Setup(x => x.VerifyFileIdIsForValidFileAsync(_submissionId, _fileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _systemUnderTest.Handle(command, CancellationToken.None);

        // Async
        result.IsError.Should().BeFalse();
        _submissionCommandRepositoryMock.Verify(x => x.Update(It.IsAny<Submission>()), Times.Never);
        _submissionEventCommandRepositoryMock.Verify(x => x.AddAsync(It.Is<SubmittedEvent>(s => s.SubmissionId == _submissionId && s.UserId == _userId && s.FileId == _fileId)), Times.Once);
        _submissionContextMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _loggerMock.VerifyLog(x => x.LogInformation("Submission with id {submissionId} submitted by user {userId}.", _submissionId, _userId));
    }

    [TestMethod]
    public async Task Handle_ReturnsError_WhenAnExceptionIsThrownSavingDatabaseChanges()
    {
        // Arrange
        var command = new SubmissionSubmitCommand { SubmissionId = _submissionId, UserId = _userId, FileId = _fileId };
        var submission = new Submission { Id = _submissionId, SubmissionType = SubmissionType.Producer, IsSubmitted = false };
        var exception = new DbUpdateException();

        _submissionQueryRepositoryMock
            .Setup(x => x.GetByIdAsync(_submissionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(submission);
        _pomSubmissionEventHelperMock
            .Setup(x => x.VerifyFileIdIsForValidFileAsync(_submissionId, _fileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _submissionContextMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(exception);

        // Act
        var result = await _systemUnderTest.Handle(command, CancellationToken.None);

        // Async
        result.IsError.Should().BeTrue();
        result.Errors.Should().OnlyContain(error => error.Type == ErrorType.Unexpected);
        _submissionCommandRepositoryMock.Verify(x => x.Update(It.IsAny<Submission>()), Times.Once);
        _submissionEventCommandRepositoryMock.Verify(x => x.AddAsync(It.IsAny<SubmittedEvent>()), Times.Once);
        _submissionContextMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _loggerMock.VerifyLog(x => x.LogCritical(exception, "An error occurred when submitting the submission with id {submissionId}", _submissionId));
    }

    [TestMethod]
    public async Task Handle_DoesNotMakeDatabaseChangesAndReturnsAnError_WhenFileIdIsNotForAValidFile()
    {
        // Arrange
        var command = new SubmissionSubmitCommand { SubmissionId = _submissionId, UserId = _userId, FileId = _fileId };
        var submission = new Submission { Id = _submissionId, SubmissionType = SubmissionType.Producer, IsSubmitted = false };

        _submissionQueryRepositoryMock
            .Setup(x => x.GetByIdAsync(_submissionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(submission);
        _pomSubmissionEventHelperMock
            .Setup(x => x.VerifyFileIdIsForValidFileAsync(_submissionId, _fileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _systemUnderTest.Handle(command, CancellationToken.None);

        // Async
        result.IsError.Should().BeTrue();
        result.Errors.Should().OnlyContain(error => error.Type == ErrorType.Failure);
        _submissionCommandRepositoryMock.Verify(x => x.Update(It.IsAny<Submission>()), Times.Never);
        _submissionEventCommandRepositoryMock.Verify(x => x.AddAsync(It.IsAny<SubmittedEvent>()), Times.Never);
        _submissionContextMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [TestMethod]
    public async Task Handle_DoesNotCreateProtectiveMonitoringEvent_WhenAnExceptionIsThrownWhenSubmitting()
    {
        // Arrange
        var command = new SubmissionSubmitCommand { SubmissionId = _submissionId, UserId = _userId, FileId = _fileId };
        var submission = new Submission { Id = _submissionId, IsSubmitted = false };

        _submissionQueryRepositoryMock
            .Setup(x => x.GetByIdAsync(_submissionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(submission);
        _pomSubmissionEventHelperMock
            .Setup(x => x.VerifyFileIdIsForValidFileAsync(_submissionId, _fileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _submissionContextMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DbUpdateException());

        // Act
        await _systemUnderTest.Handle(command, CancellationToken.None);

        // Async
        _loggingServiceMock.Verify(x => x.SendEventAsync(It.IsAny<Guid>(), It.IsAny<ProtectiveMonitoringEvent>()), Times.Never);
    }

    [TestMethod]
    public async Task Handle_CreatesProtectiveMonitoringEvent_WhenNoExceptionsAreThrownWhenSubmitting()
    {
        // Arrange
        var command = new SubmissionSubmitCommand { SubmissionId = _submissionId, UserId = _userId, FileId = _fileId };
        var submission = new Submission { Id = _submissionId, SubmissionType = SubmissionType.Producer, IsSubmitted = false };

        _submissionQueryRepositoryMock
            .Setup(x => x.GetByIdAsync(_submissionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(submission);
        _pomSubmissionEventHelperMock
            .Setup(x => x.VerifyFileIdIsForValidFileAsync(_submissionId, _fileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        await _systemUnderTest.Handle(command, CancellationToken.None);

        // Async
        _loggingServiceMock.Verify(
            s => s.SendEventAsync(
                _userId,
                It.Is<ProtectiveMonitoringEvent>(x =>
                    x.SessionId == _submissionId
                    && x.Component == "epr_pom_api_submission_status"
                    && x.PmcCode == PmcCodes.Code0212
                    && x.Priority == Priorities.NormalEvent
                    && x.TransactionCode == TransactionCodes.SubmissionSubmitted
                    && x.Message == "Submission submitted"
                    && x.AdditionalInfo == $"SubmissionId: {_submissionId}, FileId: {_fileId}")),
            Times.Once);
    }

    [TestMethod]
    public async Task Handle_Logs_WhenAnExceptionWasThrownCreatingTheProtectiveMonitoringEvent()
    {
        // Arrange
        var command = new SubmissionSubmitCommand { SubmissionId = _submissionId, UserId = _userId, FileId = _fileId };
        var submission = new Submission { Id = _submissionId, SubmissionType = SubmissionType.Producer, IsSubmitted = false };
        var exception = new Exception();

        _submissionQueryRepositoryMock
            .Setup(x => x.GetByIdAsync(_submissionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(submission);
        _pomSubmissionEventHelperMock
            .Setup(x => x.VerifyFileIdIsForValidFileAsync(_submissionId, _fileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _loggingServiceMock
            .Setup(x => x.SendEventAsync(It.IsAny<Guid>(), It.IsAny<ProtectiveMonitoringEvent>()))
            .ThrowsAsync(exception);

        // Act
        await _systemUnderTest.Handle(command, CancellationToken.None);

        // Async
        _loggerMock.VerifyLog(x => x.LogError(exception, "An error occurred creating the protective monitoring event"));
    }

    [TestMethod]
    public async Task Handle_CallsCorrectSubmissionHelper_WhenSubmissionTypeIsPom()
    {
        // Arrange
        var command = new SubmissionSubmitCommand { SubmissionId = _submissionId, UserId = _userId, FileId = _fileId };
        var submission = new Submission { Id = _submissionId, SubmissionType = SubmissionType.Producer };

        _submissionQueryRepositoryMock
            .Setup(x => x.GetByIdAsync(_submissionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(submission);

        // Act
        await _systemUnderTest.Handle(command, CancellationToken.None);

        // Async
        _pomSubmissionEventHelperMock.Verify(x => x.VerifyFileIdIsForValidFileAsync(_submissionId, _fileId, It.IsAny<CancellationToken>()), Times.Once);
        _submissionEventValidatorMock.Verify(x => x.IsSubmissionValidAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [TestMethod]
    public async Task Handle_WhenSubmissionTypeIsRegistration_AndNotValid_ReturnsFailureResult()
    {
        // Arrange
        var command = new SubmissionSubmitCommand { SubmissionId = _submissionId, UserId = _userId, FileId = _fileId };
        var submission = new Submission { Id = _submissionId, SubmissionType = SubmissionType.Registration };

        _submissionQueryRepositoryMock
            .Setup(x => x.GetByIdAsync(_submissionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(submission);

        _submissionEventValidatorMock
            .Setup(x => x.IsSubmissionValidAsync(_submissionId, _fileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => false);

        // Act
        var result = await _systemUnderTest.Handle(command, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.Errors.Should().Contain(error => error.Type == ErrorType.Failure);

        _submissionEventValidatorMock.Verify(
            x => x.IsSubmissionValidAsync(_submissionId, _fileId, It.IsAny<CancellationToken>()),
            Times.Once);
        _submissionCommandRepositoryMock.Verify(x => x.Update(It.IsAny<Submission>()), Times.Never);
        _submissionEventCommandRepositoryMock.Verify(x => x.AddAsync(It.IsAny<SubmittedEvent>()), Times.Never);
        _submissionContextMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [TestMethod]
    public async Task Handle_WhenSubmissionTypeIsRegistration_AndIsValid_ReturnsSuccessfulResult()
    {
        // Arrange
        var command = new SubmissionSubmitCommand { SubmissionId = _submissionId, UserId = _userId, FileId = _fileId };
        var submission = new Submission { Id = _submissionId, SubmissionType = SubmissionType.Registration };

        _submissionQueryRepositoryMock
            .Setup(x => x.GetByIdAsync(_submissionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(submission);

        _submissionEventValidatorMock
            .Setup(x => x.IsSubmissionValidAsync(_submissionId, _fileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => true);

        // Act
        var result = await _systemUnderTest.Handle(command, CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().Be(Unit.Value);

        _submissionEventValidatorMock.Verify(
            x => x.IsSubmissionValidAsync(_submissionId, _fileId, It.IsAny<CancellationToken>()),
            Times.Once);
        _pomSubmissionEventHelperMock.Verify(
            x => x.VerifyFileIdIsForValidFileAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [TestMethod]
    public async Task Handle_WhenSubmissionIsNotSubmittedAndBothAppReferenceNumbersAreEmpty_ShouldUpdateSubmissionAndRaiseEvent()
    {
        // Arrange
        var command = new SubmissionSubmitCommand
        {
            SubmissionId = Guid.NewGuid(),
            FileId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            SubmittedBy = "test_user",
            AppReferenceNumber = string.Empty
        };

        var submission = new Submission
        {
            Id = command.SubmissionId,
            IsSubmitted = false,
            AppReferenceNumber = string.Empty,
            SubmissionType = SubmissionType.Registration
        };

        _submissionQueryRepositoryMock
            .Setup(repo => repo.GetByIdAsync(command.SubmissionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(submission);

        _submissionEventValidatorMock.Setup(x => x.IsSubmissionValidAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        await _systemUnderTest.Handle(command, CancellationToken.None);

        // Assert
        _submissionCommandRepositoryMock.Verify(repo => repo.Update(It.Is<Submission>(s => s.IsSubmitted == true)), Times.Once);
        _submissionEventCommandRepositoryMock.Verify(repo => repo.AddAsync(It.IsAny<AbstractSubmissionEvent>()), Times.Once);
    }

    [TestMethod]
    public async Task Handle_WhenSubmissionIsSubmittedAndBothAppReferenceNumbersAreEmpty_ShouldNOTUpdateSubmissionAndShouldRaiseEvent()
    {
        // Arrange
        var command = new SubmissionSubmitCommand
        {
            SubmissionId = Guid.NewGuid(),
            FileId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            SubmittedBy = "test_user",
            AppReferenceNumber = string.Empty
        };

        var submission = new Submission
        {
            Id = command.SubmissionId,
            IsSubmitted = true,
            AppReferenceNumber = string.Empty,
            SubmissionType = SubmissionType.Registration
        };

        _submissionQueryRepositoryMock
            .Setup(repo => repo.GetByIdAsync(command.SubmissionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(submission);

        _submissionEventValidatorMock.Setup(x => x.IsSubmissionValidAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        await _systemUnderTest.Handle(command, CancellationToken.None);

        // Assert
        _submissionCommandRepositoryMock.Verify(repo => repo.Update(It.IsAny<Submission>()), Times.Never);
        _submissionEventCommandRepositoryMock.Verify(repo => repo.AddAsync(It.IsAny<AbstractSubmissionEvent>()), Times.Once);
    }

    [TestMethod]
    public async Task Handle_WhenSubmissionIsNotSubmittedAndBothAppReferenceNumbersAreNotEmpty_ShouldUpdateSubmissionAndRaiseEvent()
    {
        // Arrange
        var command = new SubmissionSubmitCommand
        {
            SubmissionId = Guid.NewGuid(),
            FileId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            SubmittedBy = "test_user",
            AppReferenceNumber = "COMMAND_APP_REF"
        };

        var submission = new Submission
        {
            Id = command.SubmissionId,
            IsSubmitted = false,
            AppReferenceNumber = "SUBMISSION_APP_REF",
            SubmissionType = SubmissionType.Registration
        };

        _submissionQueryRepositoryMock
            .Setup(repo => repo.GetByIdAsync(command.SubmissionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(submission);

        _submissionEventValidatorMock.Setup(x => x.IsSubmissionValidAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        await _systemUnderTest.Handle(command, CancellationToken.None);

        // Assert
        _submissionCommandRepositoryMock.Verify(repo => repo.Update(It.IsAny<Submission>()), Times.Exactly(2));
        _submissionEventCommandRepositoryMock.Verify(repo => repo.AddAsync(It.Is<AbstractSubmissionEvent>(e => e.SubmissionId == command.SubmissionId)), Times.Once);
    }

    [TestMethod]
    public async Task Handle_WhenSubmissionIsSubmittedAndAppReferenceNumberInCommandIsNotEmpty_ShouldUpdateSubmissionAndDoesNOTRaiseEvent()
    {
        // Arrange
        var command = new SubmissionSubmitCommand
        {
            SubmissionId = Guid.NewGuid(),
            FileId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            SubmittedBy = "test_user",
            AppReferenceNumber = "NEW_APP_REF"
        };

        var submission = new Submission
        {
            Id = command.SubmissionId,
            IsSubmitted = true,
            AppReferenceNumber = string.Empty,
            SubmissionType = SubmissionType.Registration
        };

        _submissionQueryRepositoryMock
            .Setup(repo => repo.GetByIdAsync(command.SubmissionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(submission);

        _submissionEventValidatorMock.Setup(x => x.IsSubmissionValidAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        await _systemUnderTest.Handle(command, CancellationToken.None);

        // Assert
        _submissionCommandRepositoryMock.Verify(repo => repo.Update(It.Is<Submission>(s => s.AppReferenceNumber == "NEW_APP_REF")), Times.Once);
        _submissionEventCommandRepositoryMock.Verify(repo => repo.AddAsync(It.IsAny<AbstractSubmissionEvent>()), Times.Never);
    }

    [TestMethod]
    public async Task Handle_WhenSubmissionIsSubmittedAndBothAppReferenceNumbersAreNotEmpty_ShouldUpdateSubmissionAndDoesNOTRaiseEvent()
    {
        // Arrange
        var command = new SubmissionSubmitCommand
        {
            SubmissionId = Guid.NewGuid(),
            FileId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            SubmittedBy = "test_user",
            AppReferenceNumber = "COMMAND_APP_REF"
        };

        var submission = new Submission
        {
            Id = command.SubmissionId,
            IsSubmitted = true,
            AppReferenceNumber = "SUBMISSION_APP_REF",
            SubmissionType = SubmissionType.Registration
        };

        _submissionQueryRepositoryMock
            .Setup(repo => repo.GetByIdAsync(command.SubmissionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(submission);

        _submissionEventValidatorMock.Setup(x => x.IsSubmissionValidAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        await _systemUnderTest.Handle(command, CancellationToken.None);

        // Assert
        _submissionCommandRepositoryMock.Verify(repo => repo.Update(It.IsAny<Submission>()), Times.Once);
        _submissionEventCommandRepositoryMock.Verify(repo => repo.AddAsync(It.Is<AbstractSubmissionEvent>(e => e.SubmissionId == command.SubmissionId)), Times.Never);
    }
}