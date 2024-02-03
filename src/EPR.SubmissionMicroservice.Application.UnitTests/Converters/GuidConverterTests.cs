using EPR.SubmissionMicroservice.Application.Converters;
using Newtonsoft.Json;

namespace EPR.SubmissionMicroservice.Application.UnitTests.Converters
{
    [TestClass]
    public class GuidConverterTests
    {
        private readonly GuidConverter _systemUnderTest;
        private readonly JsonSerializer _jsonSerializer;

        public GuidConverterTests()
        {
            _systemUnderTest = new GuidConverter();
            _jsonSerializer = new JsonSerializer();
        }

        [TestMethod]
        public async Task ReadJson_WhenValidGuid_ReturnsGuid()
        {
            // Arrange
            var guid = Guid.NewGuid();
            var jsonValue = $"\"{guid}\"";
            var reader = new JsonTextReader(new StringReader(jsonValue));

            // Act
            var result = _systemUnderTest.ReadJson(reader, typeof(Guid), null, _jsonSerializer);

            // Assert
            result.Should().Be(guid);
        }

        [TestMethod]
        public async Task ReadJson_WhenInvalidGuid_ReturnsDefaultGuid()
        {
            // Arrange
            var invalidGuid = string.Empty;
            var jsonValue = $"\"{invalidGuid}\"";
            var reader = new JsonTextReader(new StringReader(jsonValue));

            // Act
            var result = _systemUnderTest.ReadJson(reader, typeof(Guid), null, _jsonSerializer);

            // Assert
            result.Should().Be(Guid.Empty);
        }

        [TestMethod]
        public async Task Create_ReturnsDefaultGuid()
        {
            // Act
            var result = _systemUnderTest.Create(typeof(Guid));

            // Assert
            result.Should().Be(Guid.Empty);
        }
    }
}