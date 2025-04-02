namespace EPR.SubmissionMicroservice.Application.UnitTests.Features.Commands.SubmissionEventCreate;

using AutoMapper;
using EPR.SubmissionMicroservice.Application.Mapping;
using EPR.SubmissionMicroservice.Data.Entities.SubmissionEvent;
using EPR.SubmissionMicroservice.Data.Entities.ValidationEventError;
using EPR.SubmissionMicroservice.Data.Entities.ValidationEventWarning;
using FluentAssertions;
using TestSupport;
using TestSupport.Helpers;

[TestClass]
public class RegistrationCommandEventMappingTests
{
    [TestMethod]
    public void Mapping_WithSubmissionEventProfile_IsValidConfiguration()
    {
        // Arrange
        var mapperConfig = new MapperConfiguration(
            config => config.AddProfile(typeof(SubmissionEventProfile)));

        // Act

        // Assert
        mapperConfig.AssertConfigurationIsValid();
    }

    [TestMethod]
    public void Map_WithValidCommand_ValidateAbstractTypesMapToConcreteTypes()
    {
        // Arrange
        var mapper = AutoMapperHelpers.GetMapper();
        var command = TestCommands.SubmissionEvent.ValidRegistrationValidationEventCreateCommand();

        // Act
        var submissionEvent = mapper.Map<AbstractSubmissionEvent>(command);

        // Assert
        submissionEvent.Should().NotBeNull();
        submissionEvent.Should().BeOfType<RegistrationValidationEvent>();
        var registrationEvent = (RegistrationValidationEvent)submissionEvent;
        registrationEvent.ValidationErrors.Should().BeEmpty();
    }

    [TestMethod]
    public void Map_WithInvalidCommand_WithColumnErrors_ValidateColumnValidationErrorsMapped()
    {
        // Arrange
        int expectedColumnIndex = 0;
        string expectedColumnName = "organisation_id";
        string expectedColumnErrorCode = "801";
        var mapper = AutoMapperHelpers.GetMapper();
        var command = TestCommands.SubmissionEvent.InvalidRegistrationValidationEventCreateCommand();

        // Act
        var submissionEvent = mapper.Map<AbstractSubmissionEvent>(command);

        // Assert
        submissionEvent.Should().NotBeNull();
        submissionEvent.Should().BeOfType<RegistrationValidationEvent>();

        var registrationEvent = (RegistrationValidationEvent)submissionEvent;
        registrationEvent.ValidationErrors.Should().NotBeEmpty();

        var validationErrors = registrationEvent.ValidationErrors.OfType<RegistrationValidationError>().ToList();
        var expectedRowErrors = validationErrors.SelectMany(x => x.ColumnErrors).Count();
        registrationEvent.RowErrorCount.Should().Be(expectedRowErrors);
        registrationEvent.HasMaxRowErrors.Should().Be(false);

        foreach (var validationError in validationErrors)
        {
            validationError.OrganisationId.Should().NotBeEmpty();
            validationError.SubsidiaryId.Should().NotBeEmpty();
        }

        var columnErrors = validationErrors.SelectMany(x => x.ColumnErrors);

        columnErrors.Should().AllSatisfy(column =>
        {
            column.ColumnIndex.Should().Be(expectedColumnIndex);
            column.ColumnName.Should().Be(expectedColumnName);
            column.ErrorCode.Should().Be(expectedColumnErrorCode);
        });
    }

    [TestMethod]
    public void Map_WithValidCommand_WithColumnWarnings_ValidateColumnValidationWarningsMapped()
    {
        // Arrange
        int expectedColumnIndex = 0;
        string expectedColumnName = "organisation_id";
        string expectedColumnErrorCode = "73";
        var mapper = AutoMapperHelpers.GetMapper();
        var command = TestCommands.SubmissionEvent.RegistrationValidationEventCreateCommandWithWarnings();

        // Act
        var submissionEvent = mapper.Map<AbstractSubmissionEvent>(command);

        // Assert
        submissionEvent.Should().NotBeNull();
        submissionEvent.Should().BeOfType<RegistrationValidationEvent>();

        var registrationEvent = (RegistrationValidationEvent)submissionEvent;
        registrationEvent.ValidationErrors.Should().BeEmpty();
        registrationEvent.ValidationWarnings.Should().NotBeEmpty();

        var validationWarnings = registrationEvent.ValidationWarnings.OfType<RegistrationValidationWarning>().ToList();
        var expectedRowWarnings = validationWarnings.SelectMany(x => x.ColumnErrors).Count();
        registrationEvent.RowErrorCount.Should().Be(0);
        registrationEvent.HasMaxRowErrors.Should().Be(false);
        registrationEvent.WarningCount.Should().Be(1);

        foreach (var warnings in validationWarnings)
        {
            warnings.OrganisationId.Should().NotBeEmpty();
            warnings.SubsidiaryId.Should().NotBeEmpty();
        }

        var columnErrors = validationWarnings.SelectMany(x => x.ColumnErrors);

        columnErrors.Should().AllSatisfy(column =>
        {
            column.ColumnIndex.Should().Be(expectedColumnIndex);
            column.ColumnName.Should().Be(expectedColumnName);
            column.ErrorCode.Should().Be(expectedColumnErrorCode);
        });
    }
}