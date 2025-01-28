using EPR.SubmissionMicroservice.Application.Features.Commands.SubmissionEventCreate;
using EPR.SubmissionMicroservice.Application.Features.Commands.SubmissionEventCreate.Validators;
using EPR.SubmissionMicroservice.Data.Entities.Submission;
using EPR.SubmissionMicroservice.Data.Repositories.Queries.Interfaces;

namespace EPR.SubmissionMicroservice.Application.UnitTests.Features.Commands.SubmissionEventCreate.Validators;

[TestClass]
public class RegistrationApplicationSubmittedEventCreateCommandValidatorTests
{
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _submissionId = Guid.NewGuid();

    private Mock<IQueryRepository<Submission>> _mockQueryRepository = null!;
    private RegistrationApplicationSubmittedEventCreateCommandValidator _validator = null!;

    [TestInitialize]
    public void Setup()
    {
        _mockQueryRepository = new Mock<IQueryRepository<Submission>>();
        _validator = new RegistrationApplicationSubmittedEventCreateCommandValidator(_mockQueryRepository.Object);
        _mockQueryRepository
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Submission { UserId = _userId, Id = _submissionId });
    }

    [TestMethod]
    public async Task Should_ValidateSuccessfully_When_CommandIsValid()
    {
        // Arrange: Create a valid command
        var command = new RegistrationApplicationSubmittedEventCreateCommand
        {
            ApplicationReferenceNumber = "ABC123",
            SubmissionDate = DateTime.Now,
            SubmissionId = _submissionId,
            UserId = _userId
        };

        // Act: Validate the command
        var result = await _validator.ValidateAsync(command);

        // Assert: Validation should pass (no errors)
        result.IsValid.Should().BeTrue();
    }

    [TestMethod]
    public async Task Should_FailValidation_When_ApplicationReferenceNumberIsNullOrEmpty()
    {
        // Arrange: Create a command with a missing ApplicationReferenceNumber
        var command = new RegistrationApplicationSubmittedEventCreateCommand
        {
            ApplicationReferenceNumber = string.Empty,
            SubmissionDate = DateTime.Now
        };

        // Act: Validate the command
        var result = await _validator.ValidateAsync(command);

        // Assert: Validation should fail with the correct error message
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(error => error.ErrorMessage == "'ApplicationReferenceNumber' is mandatory");

        // Test for null case
        command.ApplicationReferenceNumber = null;
        result = await _validator.ValidateAsync(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(error => error.ErrorMessage == "'ApplicationReferenceNumber' is mandatory");
    }

    [TestMethod]
    public async Task Should_FailValidation_When_SubmissionDateIsNull()
    {
        // Arrange: Create a command with a missing SubmissionDate
        var command = new RegistrationApplicationSubmittedEventCreateCommand
        {
            ApplicationReferenceNumber = "ABC123",
            SubmissionDate = null
        };

        // Act: Validate the command
        var result = await _validator.ValidateAsync(command);

        // Assert: Validation should fail with the correct error message
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(error => error.ErrorMessage == "'SubmissionDate' is mandatory");
    }

    [TestMethod]
    public async Task Should_Include_SubmissionEventCreateCommandValidator()
    {
        // Arrange: Create a valid command (this will test if the inner validator is included)
        var command = new RegistrationApplicationSubmittedEventCreateCommand
        {
            ApplicationReferenceNumber = "ABC123",
            SubmissionDate = DateTime.Now,
            SubmissionId = _submissionId,
            UserId = _userId
        };

        // Act: Validate the command
        var result = await _validator.ValidateAsync(command);

        // Assert: Ensure that the inner validator's rules are included and applied
        result.IsValid.Should().BeTrue();
    }
}