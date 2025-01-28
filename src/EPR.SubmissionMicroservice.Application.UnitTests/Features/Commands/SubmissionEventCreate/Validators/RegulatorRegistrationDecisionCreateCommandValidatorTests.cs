using EPR.SubmissionMicroservice.Application.Features.Commands.SubmissionEventCreate.Validators;
using EPR.SubmissionMicroservice.Data.Entities.Submission;
using EPR.SubmissionMicroservice.Data.Enums;
using EPR.SubmissionMicroservice.Data.Repositories.Queries.Interfaces;

namespace EPR.SubmissionMicroservice.Application.UnitTests.Features.Commands.SubmissionEventCreate.Validators;

[TestClass]
public class RegulatorRegistrationDecisionCreateCommandValidatorTests
{
    private readonly Mock<IQueryRepository<Submission>> _mockQueryRepository;
    private readonly RegulatorRegistrationDecisionEventCreateCommandValidator _systemUnderTest;

    public RegulatorRegistrationDecisionCreateCommandValidatorTests()
    {
        _mockQueryRepository = new Mock<IQueryRepository<Submission>>();
        _systemUnderTest = new RegulatorRegistrationDecisionEventCreateCommandValidator(_mockQueryRepository.Object);
    }

    [TestMethod]
    public async Task Validator_ReturnSuccess_WhenCommandValid()
    {
        // Arrange
        var command = TestCommands.SubmissionEvent.ValidRegulatorRegistrationDecisionEventCreateCommand();

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
        var command = TestCommands.SubmissionEvent.ValidRegulatorRegistrationDecisionEventCreateCommand();
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
    public async Task Validator_ReturnErrors_WhenUserIdIsEmpty()
    {
        // Arrange
        var command = TestCommands.SubmissionEvent.ValidRegulatorRegistrationDecisionEventCreateCommand();
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
        var command = TestCommands.SubmissionEvent.ValidRegulatorRegistrationDecisionEventCreateCommand();
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
    public async Task Validator_ReturnErrors_WhenFileIdIsDefault()
    {
        // Arrange
        var command = TestCommands.SubmissionEvent.ValidRegulatorRegistrationDecisionEventCreateCommand();
        command.FileId = Guid.Empty;

        _mockQueryRepository
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Submission());

        // Act
        var result = await _systemUnderTest.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.FileId)
            .WithErrorMessage($"'File Id' must not be equal to '{Guid.Empty}'.");
    }

    [TestMethod]
    public async Task Validator_ReturnErrors_WhenRejectedAndCommentsAreEmpty()
    {
        // Arrange
        var command = TestCommands.SubmissionEvent.ValidRegulatorRegistrationDecisionEventCreateCommand();
        command.Decision = RegulatorDecision.Rejected;

        _mockQueryRepository
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Submission());

        // Act
        var result = await _systemUnderTest.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Comments)
            .WithErrorMessage("'Comments' must not be empty.");
    }

    [TestMethod]
    public async Task Validator_ReturnErrors_WhenUserSubmissionIdIsEmpty()
    {
        // Arrange
        var command = TestCommands.SubmissionEvent.ValidRegulatorRegistrationDecisionEventCreateCommand();
        command.SubmissionId = default;

        _mockQueryRepository
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Submission());

        // Act
        var result = await _systemUnderTest.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.SubmissionId)
            .WithErrorMessage("'Submission Id' must not be empty.");
    }

    [TestMethod]
    public async Task Validator_ReturnErrors_WhenUserSubmissionIdIsNotFound()
    {
        // Arrange
        var expectedGuid = Guid.NewGuid();
        var command = TestCommands.SubmissionEvent.ValidRegulatorRegistrationDecisionEventCreateCommand();
        command.SubmissionId = Guid.NewGuid();

        _mockQueryRepository
            .Setup(x => x.GetByIdAsync(command.SubmissionId, It.IsAny<CancellationToken>())).ReturnsAsync((Submission)null);

        // Act
        var result = await _systemUnderTest.TestValidateAsync(command);
        result.Should().NotBeNull();
        // Assert
        result.ShouldHaveValidationErrorFor(x => x.SubmissionId)
              .WithErrorMessage($"Submission with id {command.SubmissionId} does not exist.");
    }

    [TestMethod]
    [DataRow(default)]
    [DataRow(RegulatorDecision.None)]
    public async Task Validator_ReturnErrors_WhenDecisionIsDefaultOrNone(RegulatorDecision regulatorDecision)
    {
        // Arrange
        var command = TestCommands.SubmissionEvent.ValidRegulatorRegistrationDecisionEventCreateCommand();
        command.IsForOrganisationRegistration = true;
        command.Decision = regulatorDecision;

        _mockQueryRepository
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Submission());

        // Act
        var result = await _systemUnderTest.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Decision)
            .WithErrorMessage($"'Decision' must not be equal to '{RegulatorDecision.None}'.");
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow(" ")]
    [DataRow("")]
    public async Task Validator_ReturnError_WhenApplicationReferenceNumberIsInvalid(string appRefNum)
    {
        // Arrange
        var command = TestCommands.SubmissionEvent.ValidRegulatorRegistrationDecisionEventCreateCommand();
        command.IsForOrganisationRegistration = true;
        command.Decision = RegulatorDecision.Accepted;
        command.AppReferenceNumber = appRefNum;

        _mockQueryRepository
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Submission());

        // Act
        var result = await _systemUnderTest.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.AppReferenceNumber)
            .WithErrorMessage("'App Reference Number' must not be empty.");
    }
}
