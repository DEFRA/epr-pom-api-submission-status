﻿namespace EPR.SubmissionMicroservice.Application.UnitTests.Converters;

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
        var submissionEvent = TestRequests.SubmissionEvent.ValidRegulatorDecisionEventCreateRequest();

        // Act
        var result = submissionEvent.ToObject<AbstractSubmissionEventCreateCommand>(_systemUnderTest);

        // Assert
        result.Should().BeOfType<RegulatorPoMDecisionEventCreateCommand>();
    }
}