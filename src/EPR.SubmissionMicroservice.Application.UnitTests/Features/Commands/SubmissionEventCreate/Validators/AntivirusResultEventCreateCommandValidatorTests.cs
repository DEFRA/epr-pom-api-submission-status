namespace EPR.SubmissionMicroservice.Application.UnitTests.Features.Commands.SubmissionEventCreate.Validators;

using Application.Features.Commands.SubmissionEventCreate.Validators;
using Data.Entities.Submission;
using Data.Enums;
using Data.Repositories.Queries.Interfaces;
using FluentValidation.TestHelper;
using Moq;
using TestSupport;

[TestClass]
public class AntivirusResultEventCreateCommandValidatorTests
{
    private readonly Mock<IQueryRepository<Submission>> _mockQueryRepository;
    private readonly AntivirusResultEventCreateCommandValidator _systemUnderTest;

    public AntivirusResultEventCreateCommandValidatorTests()
    {
        _mockQueryRepository = new Mock<IQueryRepository<Submission>>();
        _systemUnderTest = new AntivirusResultEventCreateCommandValidator(_mockQueryRepository.Object);
    }

    [TestMethod]
    public async Task Validator_ReturnSuccess_WhenCommandValidForUpload()
    {
        // Arrange
        var command = TestCommands.SubmissionEvent.ValidAntivirusResultEventUploadCreateCommand();

        _mockQueryRepository
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Submission());

        // Act
        var result = await _systemUnderTest.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [TestMethod]
    public async Task Validator_ReturnSuccess_WhenCommandValidForDownload()
    {
        // Arrange
        var command = TestCommands.SubmissionEvent.ValidAntivirusResultEventDownloadCreateCommand();

        _mockQueryRepository
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Submission());

        // Act
        var result = await _systemUnderTest.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [TestMethod]
    public async Task Validator_ReturnErrors_WhenSubmissionIdIsEmptyGuid()
    {
        // Arrange
        var command = TestCommands.SubmissionEvent.ValidAntivirusResultEventUploadCreateCommand();
        command.SubmissionId = Guid.Empty;

        _mockQueryRepository
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Submission());

        // Act
        var result = await _systemUnderTest.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.SubmissionId)
            .WithErrorMessage($"'Submission Id' must not be equal to '{Guid.Empty}'.");
    }

    [TestMethod]
    public async Task Validator_ReturnErrors_WhenSubmissionDoesNotExist()
    {
        // Arrange
        var command = TestCommands.SubmissionEvent.ValidAntivirusResultEventUploadCreateCommand();

        _mockQueryRepository
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(null as Submission);

        // Act
        var result = await _systemUnderTest.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.SubmissionId)
            .WithErrorMessage($"Submission with id {command.SubmissionId} does not exist.");
    }

    [TestMethod]
    public async Task Validator_ReturnErrors_WhenUserIdIsEmpty()
    {
        // Arrange
        var command = TestCommands.SubmissionEvent.ValidAntivirusResultEventUploadCreateCommand();
        command.UserId = null;

        _mockQueryRepository
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Submission());

        // Act
        var result = await _systemUnderTest.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.UserId)
            .WithErrorMessage("'User Id' must not be empty.");
    }

    [TestMethod]
    public async Task Validator_ReturnErrors_WhenUserIdIsDefault()
    {
        // Arrange
        var command = TestCommands.SubmissionEvent.ValidAntivirusResultEventUploadCreateCommand();
        command.UserId = Guid.Empty;

        _mockQueryRepository
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Submission());

        // Act
        var result = await _systemUnderTest.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.UserId)
            .WithErrorMessage($"'User Id' must not be equal to '{Guid.Empty}'.");
    }

    [TestMethod]
    public async Task Validator_ReturnErrors_WhenFileIdIsEmpty()
    {
        // Arrange
        var command = TestCommands.SubmissionEvent.ValidAntivirusResultEventUploadCreateCommand();
        command.FileId = Guid.Empty;

        _mockQueryRepository
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Submission());

        // Act
        var result = await _systemUnderTest.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.FileId)
            .WithErrorMessage("'File Id' must not be empty.");
    }

    [TestMethod]
    public async Task Validator_ReturnErrors_WhenAntivirusScanResultIsNotDefined()
    {
        // Arrange
        var command = TestCommands.SubmissionEvent.ValidAntivirusResultEventUploadCreateCommand();
        command.AntivirusScanResult = 0;

        _mockQueryRepository
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Submission());

        // Act
        var result = await _systemUnderTest.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.AntivirusScanResult)
            .WithErrorMessage("'Antivirus Scan Result' has a range of values which does not include '0'.");
    }

    [TestMethod]
    public async Task Validator_ReturnErrors_WhenAntivirusScanTriggerIsNotDefined()
    {
        // Arrange
        var command = TestCommands.SubmissionEvent.ValidAntivirusResultEventUploadCreateCommand();
        command.AntivirusScanTrigger = 0;

        _mockQueryRepository
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Submission());

        // Act
        var result = await _systemUnderTest.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.AntivirusScanTrigger)
            .WithErrorMessage("'Antivirus Scan Trigger' has a range of values which does not include '0'.");
    }

    [TestMethod]
    public async Task Validator_ReturnErrors_WhenBlobContainerNameIsEmptyAndTriggerIsDownload()
    {
        // Arrange
        var command = TestCommands.SubmissionEvent.ValidAntivirusResultEventDownloadCreateCommand();
        command.AntivirusScanTrigger = AntivirusScanTrigger.Download;
        command.BlobContainerName = null;

        _mockQueryRepository
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Submission());

        // Act
        var result = await _systemUnderTest.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.BlobContainerName)
            .WithErrorMessage("'Blob Container Name' must not be empty.");
    }

    [TestMethod]
    public async Task Validator_ReturnErrors_WhenBlobNameIsEmptyAndTriggerIsDownload()
    {
        // Arrange
        var command = TestCommands.SubmissionEvent.ValidAntivirusResultEventDownloadCreateCommand();
        command.AntivirusScanTrigger = AntivirusScanTrigger.Download;
        command.BlobName = null;

        _mockQueryRepository
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Submission());

        // Act
        var result = await _systemUnderTest.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.BlobName)
            .WithErrorMessage("'Blob Name' must not be empty.");
    }
}