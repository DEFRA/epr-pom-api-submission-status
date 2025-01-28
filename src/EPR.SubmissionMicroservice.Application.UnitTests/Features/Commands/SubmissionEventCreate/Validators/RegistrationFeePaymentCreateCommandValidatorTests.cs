using EPR.SubmissionMicroservice.Application.Features.Commands.SubmissionEventCreate.Validators;
using EPR.SubmissionMicroservice.Data.Entities.Submission;
using EPR.SubmissionMicroservice.Data.Repositories.Queries.Interfaces;

namespace EPR.SubmissionMicroservice.Application.UnitTests.Features.Commands.SubmissionEventCreate.Validators;

[TestClass]
public class RegistrationFeePaymentCreateCommandValidatorTests
{
    private readonly Mock<IQueryRepository<Submission>> _mockQueryRepository;
    private readonly RegistrationFeePaymentEventCreateCommandValidator _systemUnderTest;

    public RegistrationFeePaymentCreateCommandValidatorTests()
    {
        _mockQueryRepository = new Mock<IQueryRepository<Submission>>();
        _systemUnderTest = new RegistrationFeePaymentEventCreateCommandValidator(_mockQueryRepository.Object);
    }

    [TestMethod]
    public async Task Validator_ReturnSuccess_WhenCommandValid()
    {
        // Arrange
        var expectedGuid = Guid.NewGuid();
        var command = TestCommands.SubmissionEvent.ValidRegistrationFeePaymentEventCreateCommand();
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
        var command = TestCommands.SubmissionEvent.ValidRegistrationFeePaymentEventCreateCommand();
        command.UserId = Guid.Empty;

        _mockQueryRepository
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Submission());

        // Act
        var result = await _systemUnderTest.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.UserId)
            .WithErrorMessage("'User Id' must not be equal to '00000000-0000-0000-0000-000000000000'.");
    }

    [TestMethod]
    public async Task Validator_ReturnErrors_WhenUserIdIsNull()
    {
        // Arrange
        var command = TestCommands.SubmissionEvent.ValidRegistrationFeePaymentEventCreateCommand();
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
    public async Task Validator_ReturnErrors_WhenUserSubmissionIdIsInvalid()
    {
        // Arrange
        var command = TestCommands.SubmissionEvent.ValidRegistrationFeePaymentEventCreateCommand();
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
        var command = TestCommands.SubmissionEvent.ValidRegistrationFeePaymentEventCreateCommand();
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
    [DataRow(null)]
    [DataRow("")]
    [DataRow(" ")]
    public async Task Validator_ReturnErrors_WhenPaymentMethodIsInvalid(string paymentMethod)
    {
        // Arrange
        var command = TestCommands.SubmissionEvent.ValidRegistrationFeePaymentEventCreateCommand();
        command.PaymentMethod = paymentMethod;

        _mockQueryRepository
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Submission());

        // Act
        var result = await _systemUnderTest.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.PaymentMethod)
            .WithErrorMessage("'PaymentMethod' is mandatory");
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow(" ")]
    public async Task Validator_ReturnErrors_WhenPaymentStatusIsInvalid(string paymentStatus)
    {
        // Arrange
        var command = TestCommands.SubmissionEvent.ValidRegistrationFeePaymentEventCreateCommand();
        command.PaymentStatus = paymentStatus;

        _mockQueryRepository
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Submission());

        // Act
        var result = await _systemUnderTest.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.PaymentStatus)
            .WithErrorMessage("'PaymentStatus' is mandatory");
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow(" ")]
    public async Task Validator_ReturnErrors_WhenPaidAmountIsInvalid(string paidAmount)
    {
        // Arrange
        var command = TestCommands.SubmissionEvent.ValidRegistrationFeePaymentEventCreateCommand();
        command.PaidAmount = paidAmount;

        _mockQueryRepository
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Submission());

        // Act
        var result = await _systemUnderTest.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.PaidAmount)
            .WithErrorMessage("'PaidAmount' is mandatory");
    }
}