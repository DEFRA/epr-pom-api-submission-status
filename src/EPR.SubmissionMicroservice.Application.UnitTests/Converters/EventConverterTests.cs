namespace EPR.SubmissionMicroservice.Application.UnitTests.Converters;

using Application.Converters;
using Application.Features.Commands.SubmissionEventCreate;
using FluentAssertions;
using Newtonsoft.Json;
using TestSupport;

[TestClass]
public class EventConverterTests
{
    private readonly JsonSerializer _systemUnderTest;

    public EventConverterTests()
    {
        _systemUnderTest = new JsonSerializer();
        _systemUnderTest.Converters.Add(new EventConverter());
    }

    [TestMethod]
    public async Task AntivirusCheckEventConverter()
    {
        // Arrange
        var submissionEvent = TestRequests.SubmissionEvent.ValidAntivirusCheckEventCreateRequest();

        // Act
        var result = submissionEvent.ToObject<AbstractSubmissionEventCreateCommand>(_systemUnderTest);

        // Assert
        result.Should().BeOfType<AntivirusCheckEventCreateCommand>();
    }

    [TestMethod]
    public async Task AntivirusResultEventConverter()
    {
        // Arrange
        var submissionEvent = TestRequests.SubmissionEvent.ValidAntivirusResultEventCreateRequest();

        // Act
        var result = submissionEvent.ToObject<AbstractSubmissionEventCreateCommand>(_systemUnderTest);

        // Assert
        result.Should().BeOfType<AntivirusResultEventCreateCommand>();
    }

    [TestMethod]
    public async Task AntivirusResultEventConverter_WithRequiresRowValidation_VerifyIsPopulated()
    {
        // Arrange
        var submissionEvent = TestRequests.SubmissionEvent.ValidAntivirusResultEventCreateRequestWithRequiresRowValidation();

        // Act
        var result = submissionEvent.ToObject<AbstractSubmissionEventCreateCommand>(_systemUnderTest);

        // Assert
        result.Should().BeOfType<AntivirusResultEventCreateCommand>();
        result.As<AntivirusResultEventCreateCommand>().RequiresRowValidation.Should().BeTrue();
    }

    [TestMethod]
    public async Task AntivirusResultEventConverter_WithoutRequiresRowValidation_VerifyIsNull()
    {
        // Arrange
        var submissionEvent = TestRequests.SubmissionEvent.ValidAntivirusResultEventCreateRequest();

        // Act
        var result = submissionEvent.ToObject<AbstractSubmissionEventCreateCommand>(_systemUnderTest);

        // Assert
        result.Should().BeOfType<AntivirusResultEventCreateCommand>();
        result.As<AntivirusResultEventCreateCommand>().RequiresRowValidation.Should().BeNull();
    }

    [TestMethod]
    public async Task CheckSplitterValidationEventConverter()
    {
        // Arrange
        var submissionEvent = TestRequests.SubmissionEvent.ValidCheckSplitterValidationEventCreateRequest();

        // Act
        var result = submissionEvent.ToObject<AbstractSubmissionEventCreateCommand>(_systemUnderTest);

        // Assert
        result.Should().BeOfType<CheckSplitterValidationEventCreateCommand>();
    }

    [TestMethod]
    public async Task ProducerValidationEventConverter()
    {
        // Arrange
        var submissionEvent = TestRequests.SubmissionEvent.ValidProducerValidationEventCreateRequest();

        // Act
        var result = submissionEvent.ToObject<AbstractSubmissionEventCreateCommand>(_systemUnderTest);

        // Assert
        result.Should().BeOfType<ProducerValidationEventCreateCommand>();
    }

    [TestMethod]
    public async Task RegistrationValidationEventConverter()
    {
        // Arrange
        var submissionEvent = TestRequests.SubmissionEvent.ValidRegistrationValidationEventCreateRequest();

        // Act
        var result = submissionEvent.ToObject<AbstractSubmissionEventCreateCommand>(_systemUnderTest);

        // Assert
        result.Should().BeOfType<RegistrationValidationEventCreateCommand>();
    }

    [TestMethod]
    public async Task RegistrationValidationEventConverter_WithRowErrorCount_VerifyIsPopulated()
    {
        // Arrange
        var submissionEvent = TestRequests.SubmissionEvent.ValidRegistrationValidationEventCreateRequestWithRowErrors();

        // Act
        var result = submissionEvent.ToObject<AbstractSubmissionEventCreateCommand>(_systemUnderTest);

        // Assert
        result.Should().BeOfType<RegistrationValidationEventCreateCommand>();
        result.As<RegistrationValidationEventCreateCommand>().RowErrorCount.Should().BeGreaterThan(0);
    }

    [TestMethod]
    public async Task RegistrationValidationEventConverter_WithoutRowErrorCount_VerifyIsNull()
    {
        // Arrange
        var submissionEvent = TestRequests.SubmissionEvent.ValidRegistrationValidationEventCreateRequest();

        // Act
        var result = submissionEvent.ToObject<AbstractSubmissionEventCreateCommand>(_systemUnderTest);

        // Assert
        result.Should().BeOfType<RegistrationValidationEventCreateCommand>();
        result.As<RegistrationValidationEventCreateCommand>().RowErrorCount.Should().BeNull();
        result.As<RegistrationValidationEventCreateCommand>().HasMaxRowErrors.Should().BeNull();
    }

    [TestMethod]
    public async Task RegistrationValidationEventConverter_WithoutHasMaxRowErrors_VerifyIsPopulated()
    {
        // Arrange
        var submissionEvent = TestRequests.SubmissionEvent.ValidRegistrationValidationEventCreateRequestWithRowErrors();

        // Act
        var result = submissionEvent.ToObject<AbstractSubmissionEventCreateCommand>(_systemUnderTest);

        // Assert
        result.Should().BeOfType<RegistrationValidationEventCreateCommand>();
        result.As<RegistrationValidationEventCreateCommand>().HasMaxRowErrors.Should().BeTrue();
    }

    [TestMethod]
    public async Task BrandValidationEventConverter()
    {
        // Arrange
        var submissionEvent = TestRequests.SubmissionEvent.ValidBrandValidationEventCreateRequest();

        // Act
        var result = submissionEvent.ToObject<AbstractSubmissionEventCreateCommand>(_systemUnderTest);

        // Assert
        result.Should().BeOfType<BrandValidationEventCreateCommand>();
    }

    [TestMethod]
    public async Task PartnerValidationEventConverter()
    {
        // Arrange
        var submissionEvent = TestRequests.SubmissionEvent.ValidPartnerValidationEventCreateRequest();

        // Act
        var result = submissionEvent.ToObject<AbstractSubmissionEventCreateCommand>(_systemUnderTest);

        // Assert
        result.Should().BeOfType<PartnerValidationEventCreateCommand>();
    }

    [TestMethod]
    public async Task EventConverter_UndefinedEvent()
    {
        // Arrange
        var submissionEvent = TestRequests.SubmissionEvent.ValidRegistrationValidationEventCreateRequest();
        submissionEvent["type"] = 0;

        // Act
        Action act = () => submissionEvent.ToObject<AbstractSubmissionEventCreateCommand>(_systemUnderTest);

        // Assert
        act.Should().Throw<NotImplementedException>();
    }

    [TestMethod]
    public async Task RegulatorPoMDecisionEventConverter()
    {
        // Arrange
        var submissionEvent = TestRequests.SubmissionEvent.ValidRegulatorPoMDecisionEventCreateRequest();

        // Act
        var result = submissionEvent.ToObject<AbstractSubmissionEventCreateCommand>(_systemUnderTest);

        // Assert
        result.Should().BeOfType<RegulatorPoMDecisionEventCreateCommand>();
    }

    [TestMethod]
    public async Task RegulatorRegistrationDecisionEventConverter()
    {
        // Arrange
        var submissionEvent = TestRequests.SubmissionEvent.ValidRegulatorRegistrationDecisionEventCreateRequest();

        // Act
        var result = submissionEvent.ToObject<AbstractSubmissionEventCreateCommand>(_systemUnderTest);

        // Assert
        result.Should().BeOfType<RegulatorRegistrationDecisionEventCreateCommand>();
    }
}