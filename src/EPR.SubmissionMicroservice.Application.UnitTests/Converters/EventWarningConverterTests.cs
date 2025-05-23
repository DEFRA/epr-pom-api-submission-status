﻿using EPR.SubmissionMicroservice.Application.Converters;
using EPR.SubmissionMicroservice.Application.Features.Commands.SubmissionEventCreate;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace EPR.SubmissionMicroservice.Application.UnitTests.Converters;

[TestClass]
public class EventWarningConverterTests
{
    private readonly JsonSerializer _systemUnderTest;

    public EventWarningConverterTests()
    {
        _systemUnderTest = new JsonSerializer();
        _systemUnderTest.Converters.Add(new EventWarningConverter());
    }

    [TestMethod]
    public async Task ProducerValidationEventEventErrorConverter()
    {
        // Arrange
        var request = TestRequests.SubmissionEvent.ValidProducerValidationEventCreateRequest();
        var validationError = new JObject
        {
            ["validationWarningType"] = request["type"]
        };

        // Act
        var result = validationError.ToObject<AbstractValidationEventCreateCommand.AbstractValidationWarning>(_systemUnderTest);

        // Assert
        result.Should().BeOfType<ProducerValidationEventCreateCommand.ProducerValidationWarning>();
    }

    [TestMethod]
    public async Task CheckSplitterValidationEventEventErrorConverter()
    {
        // Arrange
        var request = TestRequests.SubmissionEvent.ValidCheckSplitterValidationEventCreateRequest();
        var validationError = new JObject
        {
            ["validationWarningType"] = request["type"]
        };

        // Act
        var result = validationError.ToObject<AbstractValidationEventCreateCommand.AbstractValidationWarning>(_systemUnderTest);

        // Assert
        result.Should().BeOfType<CheckSplitterValidationEventCreateCommand.CheckSplitterValidationWarning>();
    }

    [TestMethod]
    public async Task RegistrationValidationEventErrorConverter()
    {
        // Arrange
        var request = TestRequests.SubmissionEvent.ValidRegistrationValidationEventCreateRequest();
        var validationError = new JObject
        {
            ["validationWarningType"] = request["type"]
        };

        // Act
        var result = validationError.ToObject<AbstractValidationEventCreateCommand.AbstractValidationWarning>(_systemUnderTest);

        // Assert
        result.Should().BeOfType<RegistrationValidationEventCreateCommand.RegistrationValidationWarning>();
    }

    [TestMethod]
    public async Task EventConverter_UndefinedEvent()
    {
        // Arrange
        var validationError = new JObject
        {
            ["validationWarningType"] = 0
        };

        // Act
        Action act = () => validationError.ToObject<AbstractValidationEventCreateCommand.AbstractValidationWarning>(_systemUnderTest);

        // Assert
        act.Should().Throw<NotImplementedException>();
    }
}