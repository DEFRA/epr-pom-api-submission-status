namespace EPR.SubmissionMicroservice.Application.UnitTests.Features.Queries.SubmissionsEventsGet;

using Application.Features.Queries.SubmissionsEventsGet;

[TestClass]
public class SubmissionsEventsGetQueryValidaroeTests
{
    private readonly SubmissionsEventsGetQueryValidator _systemUnderTest = new();

    [TestMethod]
    public void Validator_ContainsErrorForLastSyncTime_WhenLastSyncTimeIsMinimum()
    {
        // Arrange
        var query = new SubmissionsEventsGetQuery(Guid.NewGuid(), DateTime.MinValue);

        // Act
        var result = _systemUnderTest.TestValidate(query);

        // Assert
        result.ShouldHaveValidationErrorFor("LastSyncTime")
            .WithErrorMessage("'Last Sync Time' must not be empty.");
    }

    [TestMethod]
    public void Validator_ContainsErrorForSubmissionId_WhenSubmissionIdIsEmpty()
    {
        // Arrange
        var query = new SubmissionsEventsGetQuery(Guid.Empty, DateTime.Now);

        // Act
        var result = _systemUnderTest.TestValidate(query);

        // Assert
        result.ShouldHaveValidationErrorFor("SubmissionId")
            .WithErrorMessage("'Submission Id' must not be empty.");
    }
}
