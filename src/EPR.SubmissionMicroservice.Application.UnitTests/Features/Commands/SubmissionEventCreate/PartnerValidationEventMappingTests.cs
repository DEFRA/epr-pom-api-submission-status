using EPR.SubmissionMicroservice.Data.Entities.SubmissionEvent;

namespace EPR.SubmissionMicroservice.Application.UnitTests.Features.Commands.SubmissionEventCreate;

[TestClass]
public class PartnerValidationEventMappingTests
{
    [TestMethod]
    public void Map_WithValidCommand_ValidateAbstractTypesMapToConcreteTypes()
    {
        // Arrange
        var mapper = AutoMapperHelpers.GetMapper();
        var command = TestCommands.SubmissionEvent.ValidPartnerValidationEventCreateCommand();

        // Act
        var submissionEvent = mapper.Map<AbstractValidationEvent>(command);

        // Assert
        submissionEvent.Should().NotBeNull();
        submissionEvent.Should().BeOfType<PartnerValidationEvent>();
        submissionEvent.IsValid.Should().BeTrue();
        submissionEvent.Errors.Should().BeEmpty();
    }

    [TestMethod]
    public void Map_WithInvalidCommand_WithErrors_ValidateColumnValidationErrorsMapped()
    {
        // Arrange
        string expectedErrorCode = "802";
        var mapper = AutoMapperHelpers.GetMapper();
        var command = TestCommands.SubmissionEvent.InvalidPartnerValidationEventCreateCommand(expectedErrorCode);

        // Act
        var submissionEvent = mapper.Map<AbstractValidationEvent>(command);

        // Assert
        submissionEvent.Should().NotBeNull();
        submissionEvent.Should().BeOfType<PartnerValidationEvent>();
        submissionEvent.Errors.Should().NotBeEmpty();
        submissionEvent.Errors.Should().OnlyContain(x => x == expectedErrorCode);
    }
}