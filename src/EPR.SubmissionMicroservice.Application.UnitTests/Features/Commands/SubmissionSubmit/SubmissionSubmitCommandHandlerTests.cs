using EPR.SubmissionMicroservice.Application.Messaging.Publishing.RegistrationSubmittedForFeesCalculation;
using EPR.SubmissionMicroservice.Data.Entities.AntivirusEvents;
using EPR.SubmissionMicroservice.Data.Repositories.Commands;
using EPR.SubmissionMicroservice.Data.Repositories.Queries;

namespace EPR.SubmissionMicroservice.Application.UnitTests.Features.Commands.SubmissionSubmit;

using Application.Features.Commands.SubmissionSubmit;
using Common.Logging.Constants;
using Common.Logging.Models;
using Common.Logging.Services;
using Data.Entities.Submission;
using Data.Entities.SubmissionEvent;
using Data.Enums;
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
    private SubmissionSubmitCommandHandler _testSubmissionSubmitCommandHandler;
    private Mock<ISubmissionQueryRepository> _submissionQueryRepositoryMock;
    private Mock<ISubmissionFileValidator> _submissionFileValidatorMock;
    private Mock<ISubmissionCommandRepository> _submissionCommandRepositoryMock;
    private Mock<ILoggingService> _loggingServiceMock;
    private Mock<ILogger<SubmissionSubmitCommandHandler>> _loggerMock;
    private Mock<IPublisher> _publisherMock;

    [TestInitialize]
    public void TestInitialize()
    {
        _submissionQueryRepositoryMock = new();
        _submissionFileValidatorMock = new();
        _submissionCommandRepositoryMock = new();
        _loggingServiceMock = new Mock<ILoggingService>();
        _loggerMock = new Mock<ILogger<SubmissionSubmitCommandHandler>>();
        _publisherMock = new Mock<IPublisher>();
        _testSubmissionSubmitCommandHandler = new SubmissionSubmitCommandHandler(
            _submissionQueryRepositoryMock.Object,
            _loggerMock.Object,
            _submissionFileValidatorMock.Object,
            _submissionCommandRepositoryMock.Object,
            _loggingServiceMock.Object,
            _publisherMock.Object);
    }

    [TestMethod]
    public async Task Handle_UpdatesSubmissionAndCreatesASubmittedEvent_WhenSubmissionHasNotBeenSubmittedPreviously()
    {
        var command = new SubmissionSubmitCommand { SubmissionId = _submissionId, UserId = _userId, FileId = _fileId };
        var submission = new Submission { Id = _submissionId, SubmissionType = SubmissionType.Producer, IsSubmitted = false };
        var antivirusResultEvent = new AntivirusResultEvent { BlobName = "foo" };

        _submissionQueryRepositoryMock
            .Setup(x => x.GetSubmissionAsync(_submissionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(submission);
        _submissionFileValidatorMock
            .Setup(x => x.IsFileIdForValidFileAsync(submission, _fileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _submissionFileValidatorMock
            .Setup(x => x.GetAntivirusResultByFileIdAsync(_submissionId, _fileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(antivirusResultEvent);

        var result = await _testSubmissionSubmitCommandHandler.Handle(command, CancellationToken.None);

        result.IsError.Should().BeFalse();
        _submissionCommandRepositoryMock.Verify(x => x.UpdateSubmission(It.Is<Submission>(s => s.IsSubmitted.Value)), Times.Once);
        _submissionCommandRepositoryMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _submissionCommandRepositoryMock.Verify(x => x.AddSubmitEventAsync(It.Is<SubmittedEvent>(s => s.SubmissionId == _submissionId && s.UserId == _userId && s.FileId == _fileId)), Times.Once);
        _loggerMock.VerifyLog(x => x.LogInformation("Submission with id {submissionId} submitted by user {userId}.", _submissionId, _userId));
    }

    [TestMethod]
    public async Task Handle_UsesCurrentRegistrationJourney()
    {
        var command = new SubmissionSubmitCommand { SubmissionId = _submissionId, UserId = _userId, FileId = _fileId };
        var submission = new Submission { Id = _submissionId, SubmissionType = SubmissionType.Producer, IsSubmitted = false, RegistrationJourney = "TO_BE_USED" };
        var antivirusResultEvent = new AntivirusResultEvent { BlobName = "foo" };

        _submissionQueryRepositoryMock.Setup(x => x.GetSubmissionAsync(_submissionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(submission);
        _submissionFileValidatorMock.Setup(x => x.IsFileIdForValidFileAsync(submission, _fileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _submissionFileValidatorMock
            .Setup(x => x.GetAntivirusResultByFileIdAsync(_submissionId, _fileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(antivirusResultEvent);

        var result = await _testSubmissionSubmitCommandHandler.Handle(command, CancellationToken.None);

        result.IsError.Should().BeFalse();
        _submissionCommandRepositoryMock.Verify(x => x.UpdateSubmission(It.Is<Submission>(s => s.RegistrationJourney == "TO_BE_USED")), Times.Once);
    }
    
    [TestMethod]
    public async Task Handle_DoesNotCallsUpdateSubmissionAndCreatesASubmittedEvent_WhenSubmissionHasBeenSubmittedPreviously()
    {
        // Arrange
        var command = new SubmissionSubmitCommand { SubmissionId = _submissionId, UserId = _userId, FileId = _fileId };
        var submission = new Submission { Id = _submissionId, SubmissionType = SubmissionType.Producer, IsSubmitted = true };
        var antivirusResultEvent = new AntivirusResultEvent { BlobName = "foo" };

        _submissionQueryRepositoryMock
            .Setup(x => x.GetSubmissionAsync(_submissionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(submission);
        _submissionFileValidatorMock
            .Setup(x => x.IsFileIdForValidFileAsync(submission, _fileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _submissionFileValidatorMock
            .Setup(x => x.GetAntivirusResultByFileIdAsync(_submissionId, _fileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(antivirusResultEvent);

        // Act
        var result = await _testSubmissionSubmitCommandHandler.Handle(command, CancellationToken.None);

        // Async
        result.IsError.Should().BeFalse();
        _submissionCommandRepositoryMock.Verify(x => x.UpdateSubmission(It.IsAny<Submission>()), Times.Once);
        _submissionCommandRepositoryMock.Verify(x => x.AddSubmitEventAsync(It.Is<SubmittedEvent>(s => s.SubmissionId == _submissionId && s.UserId == _userId && s.FileId == _fileId)), Times.Once);
        _submissionCommandRepositoryMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
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
            .Setup(x => x.GetSubmissionAsync(_submissionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(submission);
        _submissionFileValidatorMock
            .Setup(x => x.IsFileIdForValidFileAsync(submission, _fileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _submissionCommandRepositoryMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(exception);

        // Act
        var result = await _testSubmissionSubmitCommandHandler.Handle(command, CancellationToken.None);

        // Async
        result.IsError.Should().BeTrue();
        result.Errors.Should().OnlyContain(error => error.Type == ErrorType.Unexpected);
        _submissionCommandRepositoryMock.Verify(x => x.UpdateSubmission(It.IsAny<Submission>()), Times.Once);
        _submissionCommandRepositoryMock.Verify(x => x.AddSubmitEventAsync(It.IsAny<SubmittedEvent>()), Times.Once);
        _submissionCommandRepositoryMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _loggerMock.VerifyLog(x => x.LogCritical(exception, "An error occurred when submitting the submission with id {submissionId}", _submissionId));
    }

    [TestMethod]
    public async Task Handle_DoesNotMakeDatabaseChangesAndReturnsAnError_WhenFileIdIsNotForAValidFile()
    {
        // Arrange
        var command = new SubmissionSubmitCommand { SubmissionId = _submissionId, UserId = _userId, FileId = _fileId };
        var submission = new Submission { Id = _submissionId, SubmissionType = SubmissionType.Producer, IsSubmitted = false };

        _submissionQueryRepositoryMock
            .Setup(x => x.GetSubmissionAsync(_submissionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(submission);
        _submissionFileValidatorMock
            .Setup(x => x.IsFileIdForValidFileAsync(submission, _fileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _testSubmissionSubmitCommandHandler.Handle(command, CancellationToken.None);

        // Async
        result.IsError.Should().BeTrue();
        result.Errors.Should().OnlyContain(error => error.Type == ErrorType.Failure);
        _submissionCommandRepositoryMock.Verify(x => x.UpdateSubmission(It.IsAny<Submission>()), Times.Never);
        _submissionCommandRepositoryMock.Verify(x => x.AddSubmitEventAsync(It.IsAny<SubmittedEvent>()), Times.Never);
        _submissionCommandRepositoryMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [TestMethod]
    public async Task Handle_DoesNotCreateProtectiveMonitoringEvent_WhenAnExceptionIsThrownWhenSubmitting()
    {
        // Arrange
        var command = new SubmissionSubmitCommand { SubmissionId = _submissionId, UserId = _userId, FileId = _fileId };
        var submission = new Submission { Id = _submissionId, IsSubmitted = false };

        _submissionQueryRepositoryMock
            .Setup(x => x.GetSubmissionAsync(_submissionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(submission);
        _submissionFileValidatorMock
            .Setup(x => x.IsFileIdForValidFileAsync(submission, _fileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _submissionCommandRepositoryMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DbUpdateException());

        // Act
        await _testSubmissionSubmitCommandHandler.Handle(command, CancellationToken.None);

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
            .Setup(x => x.GetSubmissionAsync(_submissionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(submission);
        _submissionFileValidatorMock
            .Setup(x => x.IsFileIdForValidFileAsync(submission, _fileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        await _testSubmissionSubmitCommandHandler.Handle(command, CancellationToken.None);

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
            .Setup(x => x.GetSubmissionAsync(_submissionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(submission);
        _submissionFileValidatorMock
            .Setup(x => x.IsFileIdForValidFileAsync(submission, _fileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _loggingServiceMock
            .Setup(x => x.SendEventAsync(It.IsAny<Guid>(), It.IsAny<ProtectiveMonitoringEvent>()))
            .ThrowsAsync(exception);

        // Act
        await _testSubmissionSubmitCommandHandler.Handle(command, CancellationToken.None);

        // Async
        _loggerMock.VerifyLog(x => x.LogError(exception, "An error occurred creating the protective monitoring event"));
    }

    [TestMethod]
    public async Task Handle_WhenSubmissionTypeIsRegistration_AndNotValid_ReturnsFailureResult()
    {
        // Arrange
        var command = new SubmissionSubmitCommand { SubmissionId = _submissionId, UserId = _userId, FileId = _fileId };
        var submission = new Submission { Id = _submissionId, SubmissionType = SubmissionType.Registration };

        _submissionQueryRepositoryMock
            .Setup(x => x.GetSubmissionAsync(_submissionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(submission);

        _submissionFileValidatorMock
            .Setup(x => x.IsFileIdForValidFileAsync(submission, _fileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => false);

        // Act
        var result = await _testSubmissionSubmitCommandHandler.Handle(command, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.Errors.Should().Contain(error => error.Type == ErrorType.Failure);

        _submissionFileValidatorMock.Verify(
            x => x.IsFileIdForValidFileAsync(submission, _fileId, It.IsAny<CancellationToken>()),
            Times.Once);
        _submissionCommandRepositoryMock.Verify(x => x.UpdateSubmission(It.IsAny<Submission>()), Times.Never);
        _submissionCommandRepositoryMock.Verify(x => x.AddSubmitEventAsync(It.IsAny<SubmittedEvent>()), Times.Never);
        _submissionCommandRepositoryMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [TestMethod]
    public async Task Handle_WhenSubmissionTypeIsRegistration_AndIsValid_ReturnsSuccessfulResult()
    {
        // Arrange
        var command = new SubmissionSubmitCommand
        {
            SubmissionId = _submissionId,
            UserId = _userId,
            FileId = _fileId,
            RegulatorNation = "GB-ENG",
            AppReferenceNumber = "PEPR00000123",
        };
        var submission = new Submission { Id = _submissionId, SubmissionType = SubmissionType.Registration };
        var antivirusResultEvent = new AntivirusResultEvent { BlobName = "foo" };

        _submissionQueryRepositoryMock
            .Setup(x => x.GetSubmissionAsync(_submissionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(submission);

        _submissionFileValidatorMock
            .Setup(x => x.IsFileIdForValidFileAsync(submission, _fileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => true);
        _submissionFileValidatorMock
            .Setup(x => x.GetAntivirusResultByFileIdAsync(_submissionId, _fileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(antivirusResultEvent);

        // Act
        var result = await _testSubmissionSubmitCommandHandler.Handle(command, CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().Be(Unit.Value);
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
            .Setup(repo => repo.GetSubmissionAsync(command.SubmissionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(submission);

        _submissionFileValidatorMock.Setup(x => x.IsFileIdForValidFileAsync(It.IsAny<Submission>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        await _testSubmissionSubmitCommandHandler.Handle(command, CancellationToken.None);

        // Assert
        _submissionCommandRepositoryMock.Verify(repo => repo.UpdateSubmission(It.Is<Submission>(s => s.IsSubmitted == true)), Times.Once);
        _submissionCommandRepositoryMock.Verify(repo => repo.AddSubmitEventAsync(It.IsAny<SubmittedEvent>()), Times.Once);
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
            .Setup(repo => repo.GetSubmissionAsync(command.SubmissionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(submission);

        _submissionFileValidatorMock.Setup(x => x.IsFileIdForValidFileAsync(It.IsAny<Submission>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        await _testSubmissionSubmitCommandHandler.Handle(command, CancellationToken.None);

        // Assert
        _submissionCommandRepositoryMock.Verify(repo => repo.UpdateSubmission(It.IsAny<Submission>()), Times.Once);
        _submissionCommandRepositoryMock.Verify(repo => repo.AddSubmitEventAsync(It.IsAny<SubmittedEvent>()), Times.Once);
    }

    [TestMethod]
    public async Task Handle_PublishesNotificationWithRegistrationBlobName_WhenBrandFileWasUploadedMoreRecentlyThanRegistrationCsv()
    {
        // Arrange — submit carries the registration file id; the brand file uploaded after it must not leak into the message
        var command = new SubmissionSubmitCommand
        {
            SubmissionId = _submissionId,
            UserId = _userId,
            FileId = _fileId,
            RegulatorNation = "GB-ENG",
            AppReferenceNumber = "PEPR00000123",
        };
        var submission = new Submission
        {
            Id = _submissionId,
            SubmissionType = SubmissionType.Registration,
            IsSubmitted = false,
            ComplianceSchemeId = Guid.NewGuid(),
        };
        const string registrationBlob = "registration-blob.csv";
        var registrationAntivirusResult = new AntivirusResultEvent { FileId = _fileId, BlobName = registrationBlob };

        _submissionQueryRepositoryMock
            .Setup(x => x.GetSubmissionAsync(_submissionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(submission);
        _submissionFileValidatorMock
            .Setup(x => x.IsFileIdForValidFileAsync(submission, _fileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _submissionFileValidatorMock
            .Setup(x => x.GetAntivirusResultByFileIdAsync(_submissionId, _fileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(registrationAntivirusResult);

        // Act
        var result = await _testSubmissionSubmitCommandHandler.Handle(command, CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        _publisherMock.Verify(
            p => p.Publish(
                It.Is<RegistrationSubmittedForFeesCalculationNotification>(n =>
                    n.SubmissionId == _submissionId
                    && n.RegistrationBlobName == registrationBlob
                    && n.ComplianceSchemeId == submission.ComplianceSchemeId),
                It.IsAny<CancellationToken>()),
            Times.Once);
        _submissionFileValidatorMock.Verify(
            x => x.GetAntivirusResultByFileIdAsync(_submissionId, _fileId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [TestMethod]
    public async Task Handle_WhenNotifyPaymentServiceFalse_DoesNotPublishFeesCalculationNotification_ButStillRecordsSubmission()
    {
        // Arrange — the frontend flags this Registration submit as one whose submission id has no
        // payment-service snapshot (legacy Synapse-only), so the service-bus publish must be suppressed
        // while the submitted-event and submission update still happen.
        var command = new SubmissionSubmitCommand
        {
            SubmissionId = _submissionId,
            UserId = _userId,
            FileId = _fileId,
            RegulatorNation = "GB-ENG",
            AppReferenceNumber = "PEPR00000123",
            NotifyPaymentService = false,
        };
        var submission = new Submission
        {
            Id = _submissionId,
            SubmissionType = SubmissionType.Registration,
            IsSubmitted = false,
        };
        var antivirusResultEvent = new AntivirusResultEvent { BlobName = "registration-blob.csv" };

        _submissionQueryRepositoryMock
            .Setup(x => x.GetSubmissionAsync(_submissionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(submission);
        _submissionFileValidatorMock
            .Setup(x => x.IsFileIdForValidFileAsync(submission, _fileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _submissionFileValidatorMock
            .Setup(x => x.GetAntivirusResultByFileIdAsync(_submissionId, _fileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(antivirusResultEvent);

        // Act
        var result = await _testSubmissionSubmitCommandHandler.Handle(command, CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        _publisherMock.Verify(
            p => p.Publish(It.IsAny<RegistrationSubmittedForFeesCalculationNotification>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _submissionCommandRepositoryMock.Verify(x => x.UpdateSubmission(It.Is<Submission>(s => s.IsSubmitted.Value)), Times.Once);
        _submissionCommandRepositoryMock.Verify(x => x.AddSubmitEventAsync(It.Is<SubmittedEvent>(s => s.SubmissionId == _submissionId)), Times.Once);
        _submissionCommandRepositoryMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public async Task Handle_WhenNotifyPaymentServiceTrue_PublishesFeesCalculationNotification()
    {
        // Arrange
        var command = new SubmissionSubmitCommand
        {
            SubmissionId = _submissionId,
            UserId = _userId,
            FileId = _fileId,
            RegulatorNation = "GB-ENG",
            AppReferenceNumber = "PEPR00000123",
            NotifyPaymentService = true,
        };
        var submission = new Submission
        {
            Id = _submissionId,
            SubmissionType = SubmissionType.Registration,
            IsSubmitted = false,
        };
        var antivirusResultEvent = new AntivirusResultEvent { BlobName = "registration-blob.csv" };

        _submissionQueryRepositoryMock
            .Setup(x => x.GetSubmissionAsync(_submissionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(submission);
        _submissionFileValidatorMock
            .Setup(x => x.IsFileIdForValidFileAsync(submission, _fileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _submissionFileValidatorMock
            .Setup(x => x.GetAntivirusResultByFileIdAsync(_submissionId, _fileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(antivirusResultEvent);

        // Act
        var result = await _testSubmissionSubmitCommandHandler.Handle(command, CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        _publisherMock.Verify(
            p => p.Publish(It.IsAny<RegistrationSubmittedForFeesCalculationNotification>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }
}