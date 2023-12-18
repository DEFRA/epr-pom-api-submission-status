namespace EPR.SubmissionMicroservice.Application.UnitTests.Features.Commands.SubmissionEventCreate.Validators;

using Application.Features.Commands.SubmissionEventCreate.Validators;
using Data.Entities.Submission;
using Data.Repositories.Queries.Interfaces;
using FluentValidation.TestHelper;
using Moq;
using TestSupport;

[TestClass]
public class AntivirusCheckEventCreateCommandValidatorTests
{
    private readonly Mock<IQueryRepository<Submission>> _mockQueryRepository;
    private readonly AntivirusCheckEventCreateCommandValidator _systemUnderTest;

    public AntivirusCheckEventCreateCommandValidatorTests()
    {
        _mockQueryRepository = new Mock<IQueryRepository<Submission>>();
        _systemUnderTest = new AntivirusCheckEventCreateCommandValidator(_mockQueryRepository.Object);
    }

    [TestMethod]
    public async Task Validator_ReturnSuccess_WhenCommandValid()
    {
        // Arrange
        var command = TestCommands.SubmissionEvent.ValidAntivirusCheckEventCreateCommand();

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
        var command = TestCommands.SubmissionEvent.ValidAntivirusCheckEventCreateCommand();
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
        var command = TestCommands.SubmissionEvent.ValidAntivirusCheckEventCreateCommand();

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
        var command = TestCommands.SubmissionEvent.ValidAntivirusCheckEventCreateCommand();
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
        var command = TestCommands.SubmissionEvent.ValidAntivirusCheckEventCreateCommand();
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
    public async Task Validator_ReturnErrors_WhenFileNameIsEmpty()
    {
        // Arrange
        var command = TestCommands.SubmissionEvent.ValidAntivirusCheckEventCreateCommand();
        command.FileName = string.Empty;

        _mockQueryRepository
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Submission());

        // Act
        var result = await _systemUnderTest.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.FileName)
            .WithErrorMessage("'File Name' must not be empty.");
    }

    [TestMethod]
    public async Task Validator_ReturnErrors_WhenFileNameIsTooLarge()
    {
        // Arrange
        var command = TestCommands.SubmissionEvent.ValidAntivirusCheckEventCreateCommand();
        command.FileName = new string('a', 101);

        _mockQueryRepository
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Submission());

        // Act
        var result = await _systemUnderTest.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.FileName)
            .WithErrorMessage("The length of 'File Name' must be 100 characters or fewer. You entered 101 characters.");
    }

    [TestMethod]
    public async Task Validator_ReturnErrors_WhenFileTypeIsNotDefined()
    {
        // Arrange
        var command = TestCommands.SubmissionEvent.ValidAntivirusCheckEventCreateCommand();
        command.FileType = 0;

        _mockQueryRepository
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Submission());

        // Act
        var result = await _systemUnderTest.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.FileType)
            .WithErrorMessage("'File Type' has a range of values which does not include '0'.");
    }

    [TestMethod]
    public async Task Validator_ReturnErrors_WhenFileIdIsEmpty()
    {
        // Arrange
        var command = TestCommands.SubmissionEvent.ValidAntivirusCheckEventCreateCommand();
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
    public async Task Validator_DoesNotContainAnErrorForRegistrationSetId_WhenItIsNotProvided()
    {
        // Arrange
        var command = TestCommands.SubmissionEvent.ValidAntivirusCheckEventCreateCommand();

        _mockQueryRepository
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Submission());

        // Act
        var result = await _systemUnderTest.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.RegistrationSetId);
    }

    [TestMethod]
    public async Task Validator_ContainsAnErrorForRegistrationSetId_WhenItIsAnEmptyGuid()
    {
        // Arrange
        var command = TestCommands.SubmissionEvent.ValidAntivirusCheckEventCreateCommand();
        command.RegistrationSetId = Guid.Empty;

        _mockQueryRepository
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Submission());

        // Act
        var result = await _systemUnderTest.TestValidateAsync(command);

        // Assert
        result
            .ShouldHaveValidationErrorFor(x => x.RegistrationSetId)
            .WithErrorMessage("'Registration Set Id' must not be equal to '00000000-0000-0000-0000-000000000000'.");
    }
}