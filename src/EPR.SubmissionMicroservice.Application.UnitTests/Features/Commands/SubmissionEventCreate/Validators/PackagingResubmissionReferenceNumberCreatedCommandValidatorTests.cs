using EPR.SubmissionMicroservice.Application.Features.Commands.SubmissionEventCreate.Validators;
using EPR.SubmissionMicroservice.Data.Entities.Submission;
using EPR.SubmissionMicroservice.Data.Repositories.Queries.Interfaces;

namespace EPR.SubmissionMicroservice.Application.UnitTests.Features.Commands.SubmissionEventCreate.Validators
{
    [TestClass]
    public class PackagingResubmissionReferenceNumberCreatedCommandValidatorTests
    {
        private readonly Mock<IQueryRepository<Submission>> _mockQueryRepository;
        private readonly PackagingResubmissionReferenceNumberCreateCommandValidator _systemUnderTest;

        public PackagingResubmissionReferenceNumberCreatedCommandValidatorTests()
        {
            _mockQueryRepository = new Mock<IQueryRepository<Submission>>();
            _systemUnderTest = new PackagingResubmissionReferenceNumberCreateCommandValidator(_mockQueryRepository.Object);
        }

        [TestMethod]
        public async Task Validator_ReturnSuccess_WhenCommandValid()
        {
            // Arrange
            var expectedGuid = Guid.NewGuid();
            var command = TestCommands.SubmissionEvent.ValidPackagingResubmissionReferenceNumberCreatedCommand();
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
            var command = TestCommands.SubmissionEvent.ValidPackagingResubmissionReferenceNumberCreatedCommand();
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
            var command = TestCommands.SubmissionEvent.ValidPackagingResubmissionReferenceNumberCreatedCommand();
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
            var command = TestCommands.SubmissionEvent.ValidPackagingResubmissionReferenceNumberCreatedCommand();
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
            var command = TestCommands.SubmissionEvent.ValidPackagingResubmissionReferenceNumberCreatedCommand();
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
        public async Task Validator_ReturnErrors_WhenPackagingResubmissionReferenceNumberIsNull(string packagingResubmissionReferenceNumber)
        {
            // Arrange
            var command = TestCommands.SubmissionEvent.ValidPackagingResubmissionReferenceNumberCreatedCommand();
            command.PackagingResubmissionReferenceNumber = packagingResubmissionReferenceNumber;

            _mockQueryRepository
                .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Submission());

            // Act
            var result = await _systemUnderTest.TestValidateAsync(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.PackagingResubmissionReferenceNumber)
                .WithErrorMessage("'Packaging Resubmission ReferenceNumber' is mandatory");
        }
    }
}