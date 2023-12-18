namespace EPR.SubmissionMicroservice.Application.UnitTests.Converters;

using Application.Converters;
using Application.Features.Commands.SubmissionEventCreate;
using Data.Enums;
using FluentAssertions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TestSupport;

[TestClass]
public class EventErrorConverterTests
{
    private readonly JsonSerializer _systemUnderTest;

    public EventErrorConverterTests()
    {
        _systemUnderTest = new JsonSerializer();
        _systemUnderTest.Converters.Add(new EventErrorConverter());
    }

    [TestMethod]
    public async Task CheckSplitterValidationEventEventErrorConverter()
    {
        // Arrange
        var request = TestRequests.SubmissionEvent.ValidCheckSplitterValidationEventCreateRequest();
        var validationError = new JObject
        {
            ["validationErrorType"] = request["type"]
        };

        // Act
        var result = validationError.ToObject<AbstractValidationEventCreateCommand.AbstractValidationError>(_systemUnderTest);

        // Assert
        result.Should().BeOfType<CheckSplitterValidationEventCreateCommand.CheckSplitterValidationError>();
    }

    [TestMethod]
    public async Task ProducerValidationEventEventErrorConverter()
    {
        // Arrange
        var request = TestRequests.SubmissionEvent.ValidProducerValidationEventCreateRequest();
        var validationError = new JObject
        {
            ["validationErrorType"] = request["type"]
        };

        // Act
        var result = validationError.ToObject<AbstractValidationEventCreateCommand.AbstractValidationError>(_systemUnderTest);

        // Assert
        result.Should().BeOfType<ProducerValidationEventCreateCommand.ProducerValidationError>();
    }

    [TestMethod]
    public async Task ConvertRegistrationValidationError_WithColumnErrors_ValidateObjectIsPopulated()
    {
        // Arrange
        string expectedOrgId = "28";
        int expectedRowIndex = 1;
        string expectedSubsidiaryId = "1";
        string expectedErrorCode = "801";
        int expectedColumnIndex = 0;
        string expectedColumnName = "organisation_id";

        var validationError = new JObject
        {
            ["validationErrorType"] = (int)EventType.Registration,
            ["rowNumber"] = expectedRowIndex,
            ["organisationId"] = expectedOrgId,
            ["subsidiaryId"] = expectedSubsidiaryId,
            ["columnErrors"] = new JArray
            {
                new JObject
                {
                    ["errorCode"] = expectedErrorCode,
                    ["columnIndex"] = expectedColumnIndex,
                    ["columnName"] = expectedColumnName,
                }
            }
        };

        // Act
        var result = validationError.ToObject<AbstractValidationEventCreateCommand.AbstractValidationError>(_systemUnderTest);

        // Assert
        result.Should().BeOfType<RegistrationValidationEventCreateCommand.RegistrationValidationError>();

        var registrationError = (RegistrationValidationEventCreateCommand.RegistrationValidationError)result;
        registrationError.ValidationErrorType.Should().Be(ValidationType.Registration);
        registrationError.OrganisationId.Should().Be(expectedOrgId);
        registrationError.SubsidiaryId.Should().Be("1");
        registrationError.ColumnErrors.Should().NotBeEmpty();

        var columnError = registrationError.ColumnErrors.First();
        columnError.ErrorCode.Should().Be("801");
        columnError.ColumnIndex.Should().Be(0);
        columnError.ColumnName.Should().Be("organisation_id");
    }

    [TestMethod]
    public async Task EventConverter_UndefinedEvent()
    {
        // Arrange
        var validationError = new JObject
        {
            ["validationErrorType"] = 0
        };

        // Act
        Action act = () => validationError.ToObject<AbstractValidationEventCreateCommand.AbstractValidationError>(_systemUnderTest);

        // Assert
        act.Should().Throw<NotImplementedException>();
    }
}