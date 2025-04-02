using EPR.SubmissionMicroservice.Application.Features.Queries.RegisterationValidationWarnings;
using EPR.SubmissionMicroservice.Data.Entities.Submission;
using EPR.SubmissionMicroservice.Data.Repositories.Queries.Interfaces;

namespace EPR.SubmissionMicroservice.Application.UnitTests.Features.Queries.RegistrationValidationWarnings;

[TestClass]
public class RegistrationValidationWarningQueryValidatorTests
{
    private readonly Mock<IQueryRepository<Submission>> _mockQueryRepository;
    private readonly RegistrationValidationWarningQueryValidator _systemUnderTest;

    public RegistrationValidationWarningQueryValidatorTests()
    {
        _mockQueryRepository = new Mock<IQueryRepository<Submission>>();
        _systemUnderTest = new RegistrationValidationWarningQueryValidator(_mockQueryRepository.Object);
    }

    [TestMethod]
    public async Task Validator_ShouldNotHaveAnyErrors_WhenValidRegistrationValidationWarningQueryProvided()
    {
        // Arrange
        var submissionId = Guid.NewGuid();
        var organisationId = Guid.NewGuid();

        _mockQueryRepository
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Submission
            {
                OrganisationId = organisationId
            });

        // Act
        var result = await _systemUnderTest.TestValidateAsync(new RegistrationValidationWarningQuery(submissionId, organisationId));

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [TestMethod]
    public async Task Validator_ShouldContainValidationError_WhenEmptySubmissionIdIsProvided()
    {
        // Arrange
        var organisationId = Guid.NewGuid();

        // Act
        var result = await _systemUnderTest.TestValidateAsync(new RegistrationValidationWarningQuery(Guid.Empty, organisationId));

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.SubmissionId);
    }

    [TestMethod]
    public async Task Validator_ReturnErrors_WhenOrganisationIdIsEmpty()
    {
        // Arrange
        var submissionId = Guid.NewGuid();

        // Act
        var result = await _systemUnderTest.TestValidateAsync(new RegistrationValidationWarningQuery(submissionId, Guid.Empty));

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.OrganisationId);
    }

    [TestMethod]
    public async Task Validator_ReturnErrors_WhenOrganisationIdIsInvalid()
    {
        // Arrange
        var submissionId = Guid.NewGuid();
        var organisationId = Guid.NewGuid();

        _mockQueryRepository
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Submission
            {
                OrganisationId = It.IsAny<Guid>()
            });

        // Act
        var result = await _systemUnderTest.TestValidateAsync(new RegistrationValidationWarningQuery(submissionId, organisationId));

        // Assert
        result.ShouldHaveAnyValidationError()
            .WithErrorMessage($"OrganisationId does not match organisation of the submission record");
    }
}