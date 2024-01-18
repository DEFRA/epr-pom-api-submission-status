using EPR.SubmissionMicroservice.Data.Entities.SubmissionEvent;

namespace EPR.SubmissionMicroservice.Application.UnitTests.Features.Commands.SubmissionEventCreate;

[TestClass]
public class BrandValidationEventMappingTests
{
    [TestMethod]
    public void Map_WithValidCommand_ValidateAbstractTypesMapToConcreteTypes()
    {
        // Arrange
        var mapper = AutoMapperHelpers.GetMapper();
        var command = TestCommands.SubmissionEvent.ValidBrandValidationEventCreateCommand();

        // Act
        var submissionEvent = mapper.Map<AbstractValidationEvent>(command);

        // Assert
        submissionEvent.Should().NotBeNull();
        submissionEvent.Should().BeOfType<BrandValidationEvent>();
        submissionEvent.IsValid.Should().BeTrue();
        var registrationEvent = (BrandValidationEvent)submissionEvent;
        registrationEvent.Errors.Should().BeEmpty();
    }

    [TestMethod]
    public void Map_WithInvalidCommand_WithErrors_ValidateColumnValidationErrorsMapped()
    {
        // Arrange
        string expectedErrorCode = "802";
        var mapper = AutoMapperHelpers.GetMapper();
        var command = TestCommands.SubmissionEvent.InvalidBrandValidationEventCreateCommand(expectedErrorCode);

        // Act
        var submissionEvent = mapper.Map<AbstractValidationEvent>(command);

        // Assert
        submissionEvent.Should().NotBeNull();
        submissionEvent.Should().BeOfType<BrandValidationEvent>();
        submissionEvent.IsValid.Should().BeFalse();

        var registrationEvent = (BrandValidationEvent)submissionEvent;
        registrationEvent.Errors.Should().NotBeEmpty();
        registrationEvent.Errors.Should().OnlyContain(x => x == expectedErrorCode);
    }
}