using EPR.SubmissionMicroservice.Application.Features.Queries.RegistrationValidationErrors;
using EPR.SubmissionMicroservice.Data.Entities.Submission;
using EPR.SubmissionMicroservice.Data.Repositories.Queries.Interfaces;

namespace EPR.SubmissionMicroservice.Application.UnitTests.Features.Queries.RegistrationValidationErrors
{
    [TestClass]
    public class RegistrationValidationErrorQueryValidatorTests
    {
        private readonly Mock<IQueryRepository<Submission>> _mockQueryRepository;
        private readonly RegistrationValidationErrorQueryValidator _systemUnderTest;

        public RegistrationValidationErrorQueryValidatorTests()
        {
            _mockQueryRepository = new Mock<IQueryRepository<Submission>>();
            _systemUnderTest = new RegistrationValidationErrorQueryValidator(_mockQueryRepository.Object);
        }

        [TestMethod]
        public async Task Validator_ShouldNotHaveAnyErrors_WhenValidRegistrationValidationErrorQueryProvided()
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
            var result = await _systemUnderTest.TestValidateAsync(new RegistrationValidationErrorQuery(submissionId, organisationId));

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
        }

        [TestMethod]
        public async Task Validator_ShouldContainValidationError_WhenEmptySubmissionIdIsProvided()
        {
            // Arrange
            var organisationId = Guid.NewGuid();

            // Act
            var result = await _systemUnderTest.TestValidateAsync(new RegistrationValidationErrorQuery(Guid.Empty, organisationId));

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.SubmissionId);
        }

        [TestMethod]
        public async Task Validator_ReturnErrors_WhenOrganisationIdIsEmpty()
        {
            // Arrange
            var submissionId = Guid.NewGuid();

            // Act
            var result = await _systemUnderTest.TestValidateAsync(new RegistrationValidationErrorQuery(submissionId, Guid.Empty));

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
            var result = await _systemUnderTest.TestValidateAsync(new RegistrationValidationErrorQuery(submissionId, organisationId));

            // Assert
            result.ShouldHaveAnyValidationError()
                .WithErrorMessage($"OrganisationId does not match organisation of the submission record");
        }
    }
}