using EPR.SubmissionMicroservice.Application.Features.Commands.SubmissionEventCreate;
using EPR.SubmissionMicroservice.Application.Features.Commands.SubmissionEventCreate.Validators;
using EPR.SubmissionMicroservice.Data.Entities.Submission;
using EPR.SubmissionMicroservice.Data.Repositories.Queries.Interfaces;

namespace EPR.SubmissionMicroservice.Application.UnitTests.Features.Commands.SubmissionEventCreate.Validators;

[TestClass]
public class PackagingResubmissionApplicationSubmittedCommandValidatorTests
{
    private readonly Mock<IQueryRepository<Submission>> _mockQueryRepository;
    private readonly PackagingResubmissionApplicationSubmittedCommandValidator _systemUnderTest;

    public PackagingResubmissionApplicationSubmittedCommandValidatorTests()
    {
        _mockQueryRepository = new Mock<IQueryRepository<Submission>>();
        _systemUnderTest = new PackagingResubmissionApplicationSubmittedCommandValidator(_mockQueryRepository.Object);
    }

    [TestMethod]
    public async Task Validate_IsResubmitted_ShouldHaveError_WhenFalse()
    {
        // Arrange
        var expectedGuid = Guid.NewGuid();
        var command = new PackagingResubmissionApplicationSubmittedCreateCommand { IsResubmitted = false };
        command.SubmissionId = expectedGuid;
        command.UserId = Guid.NewGuid();

        _mockQueryRepository
                .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Submission() { Id = expectedGuid });

        // Act
        var result = await _systemUnderTest.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.IsResubmitted)
            .WithErrorMessage("'Packaging Resubmission IsResubmitted' should be true");
    }

    [TestMethod]
    public async Task Validate_IsResubmitted_ShouldPass_WhenTrue()
    {
        // Arrange
        var expectedGuid = Guid.NewGuid();
        var command = new PackagingResubmissionApplicationSubmittedCreateCommand { IsResubmitted = true };
        command.SubmissionId = expectedGuid;
        command.UserId = Guid.NewGuid();

        _mockQueryRepository
                .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Submission() { Id = expectedGuid });

        // Act
        var result = await _systemUnderTest.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.IsResubmitted);
    }
}