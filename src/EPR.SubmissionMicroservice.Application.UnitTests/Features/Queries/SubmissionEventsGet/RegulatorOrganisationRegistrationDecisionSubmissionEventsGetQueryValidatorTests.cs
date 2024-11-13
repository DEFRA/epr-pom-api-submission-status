using EPR.SubmissionMicroservice.Application.Features.Queries.SubmissionEventsGet;

namespace EPR.SubmissionMicroservice.Application.UnitTests.Features.Queries.SubmissionEventsGet
{
    [TestClass]
    public class RegulatorOrganisationRegistrationDecisionSubmissionEventsGetQueryValidatorTests
    {
        private readonly RegulatorOrganisationRegistrationDecisionSubmissionEventsGetQueryValidator _systemUnderTest = new();

        [TestMethod]
        public void Validator_ShouldNotHaveAnyErrors_WhenValidLastSyncTimeProvided()
        {
            // Arrange
            var query = new RegulatorOrganisationRegistrationDecisionSubmissionEventsGetQuery()
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
            var query = new RegulatorOrganisationRegistrationDecisionSubmissionEventsGetQuery();

            // Act
            var result = _systemUnderTest.TestValidate(query);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.LastSyncTime);
        }
    }
}