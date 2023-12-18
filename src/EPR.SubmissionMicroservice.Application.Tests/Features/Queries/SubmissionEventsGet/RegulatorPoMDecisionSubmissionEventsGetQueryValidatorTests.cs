using EPR.SubmissionMicroservice.Application.Features.Queries.SubmissionEventsGet;
using FluentValidation.TestHelper;

namespace EPR.SubmissionMicroservice.Application.Tests.Features.Queries.SubmissionEventsGet;

[TestClass]
public class RegulatorPomDecisionSubmissionEventsGetQueryValidatorTests
{
    private readonly RegulatorPoMDecisionSubmissionEventsGetQueryValidator _systemUnderTest = new();

    [TestMethod]
    public void Validator_ContainsErrorForLastSyncTime_WhenLastSyncTimeIsNotProvided()
    {
        // Arrange
        var query = new RegulatorPoMDecisionSubmissionEventsGetQuery();

        // Act
        var result = _systemUnderTest.TestValidate(query);

        // Assert
        result.ShouldHaveValidationErrorFor("LastSyncTime")
            .WithErrorMessage("'Last Sync Time' must not be empty.");
    }

    [TestMethod]
    public void Validator_DoesNotContainAnError_WhenQueryIsValid()
    {
        // Arrange
        var query = new RegulatorPoMDecisionSubmissionEventsGetQuery
        {
           LastSyncTime = DateTime.Today
        };

        // Act
        var result = _systemUnderTest.TestValidate(query);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }
}