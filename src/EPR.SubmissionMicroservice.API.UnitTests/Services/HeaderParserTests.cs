namespace EPR.SubmissionMicroservice.API.UnitTests.Services;

using API.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

[TestClass]
public class HeaderParserTests
{
    private readonly HeaderParser _systemUnderTest;

    public HeaderParserTests()
    {
        _systemUnderTest = new HeaderParser();
    }

    [TestMethod]
    public void ValidHeaderMetadata_ReturnsValidPomHeader()
    {
        // Arrange
        var organisationId = "3fa85f64-5717-4562-b3fc-2c963f66afa6";
        var userId = "3fa85f64-5717-4562-b3fc-2c963f66afa6";

        var headerDictionary = new HeaderDictionary(
            new Dictionary<string, StringValues>()
            {
                { "OrganisationId", organisationId },
                { "UserId", userId }
            });

        // Act
        var result = _systemUnderTest.Parse(headerDictionary);

        // Assert
        result.OrganisationId.Should().Be(organisationId);
        result.UserId.Should().Be(userId);
    }

    [TestMethod]
    public void InvalidHeaderMetadata_ReturnsInvalidPomHeader()
    {
        // Arrange
        var headerDictionary = new HeaderDictionary(
            new Dictionary<string, StringValues>()
            { });

        // Act
        var result = _systemUnderTest.Parse(headerDictionary);

        // Assert
        result.OrganisationId.Should().Be(Guid.Empty);
        result.UserId.Should().Be(Guid.Empty);
    }
}