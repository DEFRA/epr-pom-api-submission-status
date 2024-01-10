using EPR.SubmissionMicroservice.Application.Features.Queries.SubmissionEventsGet;

namespace EPR.SubmissionMicroservice.Application.UnitTests.Features.Queries.SubmissionEventsGet
{
    [TestClass]
    public class RegulatorRegistrationDecisionSubmissionEventsGetQueryValidatorTests
    {
        private readonly RegulatorRegistrationDecisionSubmissionEventsGetQueryValidator _systemUnderTest = new();

        [TestMethod]
        public void Validator_ShouldNotHaveAnyErrors_WhenValidLastSyncTimeProvided()
        {
            // Arrange
            var query = new RegulatorRegistrationDecisionSubmissionEventsGetQuery()
            {
                LastSyncTime = DateTime.Now
            };

            // Act
            var result = _systemUnderTest.TestValidate(query);

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
        }

        [TestMethod]
        public void Validator_ShouldContainValidationError_WhenEmptyLastSyncTimeIsProvided()
        {
            // Arrange
            var query = new RegulatorRegistrationDecisionSubmissionEventsGetQuery();

            // Act
            var result = _systemUnderTest.TestValidate(query);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.LastSyncTime);
        }
    }
}