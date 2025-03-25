using EPR.SubmissionMicroservice.Application.Features.Commands.SubmissionEventCreate;
using EPR.SubmissionMicroservice.Application.Features.Commands.SubmissionEventCreate.Validators;
using EPR.SubmissionMicroservice.Data.Entities.Submission;
using EPR.SubmissionMicroservice.Data.Repositories.Queries.Interfaces;

namespace EPR.SubmissionMicroservice.Application.UnitTests.Features.Commands.SubmissionEventCreate.Validators;

[TestClass]
public class PackagingResubmissionFeeViewCreateCommandValidatorTests
{
    private readonly Mock<IQueryRepository<Submission>> _mockQueryRepository;
    private readonly PackagingResubmissionFeeViewCreateCommandValidator _systemUnderTest;

    public PackagingResubmissionFeeViewCreateCommandValidatorTests()
    {
        _mockQueryRepository = new Mock<IQueryRepository<Submission>>();
        _systemUnderTest = new PackagingResubmissionFeeViewCreateCommandValidator(_mockQueryRepository.Object);
    }

    [TestMethod]
    public async Task Validate_IsPackagingResubmissionFeeViewed_ShouldHaveError_WhenFalse()
    {
        // Arrange
        var expectedGuid = Guid.NewGuid();
        var command = new PackagingResubmissionFeeViewCreateCommand { IsPackagingResubmissionFeeViewed = false };
        command.SubmissionId = expectedGuid;
        command.UserId = Guid.NewGuid();

        _mockQueryRepository
                .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Submission() { Id = expectedGuid });

        // Act
        var result = await _systemUnderTest.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.IsPackagingResubmissionFeeViewed)
            .WithErrorMessage("'Packaging Resubmission IsPackagingResubmissionFeeViewed' should be true");
    }

    [TestMethod]
    public async Task Validate_IsPackagingResubmissionFeeViewed_ShouldPass_WhenTrue()
    {
        // Arrange
        var expectedGuid = Guid.NewGuid();
        var command = new PackagingResubmissionFeeViewCreateCommand { IsPackagingResubmissionFeeViewed = true };
        command.SubmissionId = expectedGuid;
        command.UserId = Guid.NewGuid();

        _mockQueryRepository
                .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Submission() { Id = expectedGuid });

        // Act
        var result = await _systemUnderTest.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.IsPackagingResubmissionFeeViewed);
    }
}
