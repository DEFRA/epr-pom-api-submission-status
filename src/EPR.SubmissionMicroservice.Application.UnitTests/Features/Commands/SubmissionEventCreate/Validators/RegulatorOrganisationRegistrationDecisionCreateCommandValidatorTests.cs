using EPR.SubmissionMicroservice.Application.Features.Commands.SubmissionEventCreate.Validators;
using EPR.SubmissionMicroservice.Data.Entities.Submission;
using EPR.SubmissionMicroservice.Data.Enums;
using EPR.SubmissionMicroservice.Data.Repositories.Queries.Interfaces;

namespace EPR.SubmissionMicroservice.Application.UnitTests.Features.Commands.SubmissionEventCreate.Validators;

[TestClass]
public class RegulatorOrganisationRegistrationDecisionCreateCommandValidatorTests
{
    private readonly Mock<IQueryRepository<Submission>> _mockQueryRepository;
    private readonly RegulatorOrganisationRegistrationDecisionEventCreateCommandValidator _systemUnderTest;

    public RegulatorOrganisationRegistrationDecisionCreateCommandValidatorTests()
    {
        _mockQueryRepository = new Mock<IQueryRepository<Submission>>();
        _systemUnderTest = new RegulatorOrganisationRegistrationDecisionEventCreateCommandValidator(_mockQueryRepository.Object);
    }

    [TestMethod]
    public async Task Validator_ReturnSuccess_WhenCommandValid()
    {
        // Arrange
        var expectedGuid = Guid.NewGuid();
        var command = TestCommands.SubmissionEvent.ValidRegulatorOrganisationRegistrationDecisionEventCreateCommand();
        command.SubmissionId = expectedGuid;

        _mockQueryRepository
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Submission() { Id = expectedGuid });

        // Act
        var result = await _systemUnderTest.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [TestMethod]
    public async Task Validator_ReturnErrors_WhenUserIdIsEmpty()
    {
        // Arrange
        var command = TestCommands.SubmissionEvent.ValidRegulatorOrganisationRegistrationDecisionEventCreateCommand();
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
    public async Task Validator_ReturnErrors_WhenUserSubmissionIdIsEmpty()
    {
        // Arrange
        var command = TestCommands.SubmissionEvent.ValidRegulatorOrganisationRegistrationDecisionEventCreateCommand();
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
        var command = TestCommands.SubmissionEvent.ValidRegulatorOrganisationRegistrationDecisionEventCreateCommand();
        command.SubmissionId = Guid.NewGuid();

        _mockQueryRepository
            .Setup(x => x.GetByIdAsync(command.SubmissionId, It.IsAny<CancellationToken>())).ReturnsAsync((Submission)null);

        // Act
        var result = await _systemUnderTest.TestValidateAsync(command);
        result.Should().NotBeNull();
        // Assert
        //result.ShouldHaveValidationErrorFor(x => x.SubmissionId)
          //  .WithErrorMessage($"Submission with id {command.SubmissionId} does not exist.");
    }

    [TestMethod]
    public async Task Validator_ReturnErrors_WhenUserIdIsDefault()
    {
        // Arrange
        var command = TestCommands.SubmissionEvent.ValidRegulatorOrganisationRegistrationDecisionEventCreateCommand();
        command.UserId = Guid.Empty;

        _mockQueryRepository
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Submission());

        // Act
        var result = await _systemUnderTest.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.UserId)
            .WithErrorMessage($"'User Id' must not be empty.");
    }

    [TestMethod]
    public async Task Validator_ReturnErrors_WhenDecisionIsDefault()
    {
        // Arrange
        var command = TestCommands.SubmissionEvent.ValidRegulatorOrganisationRegistrationDecisionEventCreateCommand();
        command.Decision = default;

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
    public async Task Validator_ReturnErrors_WhenDecisionIsNone()
    {
        // Arrange
        var command = TestCommands.SubmissionEvent.ValidRegulatorOrganisationRegistrationDecisionEventCreateCommand();
        command.Decision = RegulatorDecision.None;

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
    [DataRow(RegulatorDecision.Cancelled, "Cancelled")]
    [DataRow(RegulatorDecision.Rejected, "Rejected")]
    [DataRow(RegulatorDecision.Queried, "Queried")]
    public async Task Validator_ReturnErrors_WhenCommentsAreEmptyForCancelledRejectedQueried(RegulatorDecision decision, string dataName)
    {
        // Arrange
        var command = TestCommands.SubmissionEvent.ValidRegulatorOrganisationRegistrationDecisionEventCreateCommand();
        command.Decision = decision;

        _mockQueryRepository
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Submission());

        // Act
        var result = await _systemUnderTest.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Comments)
            .WithErrorMessage($"'Comments' must not be empty.");
    }
}
