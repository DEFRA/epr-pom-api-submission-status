namespace EPR.SubmissionMicroservice.API.UnitTests.Errors;

using API.Errors;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Options;

[TestClass]
public class CustomProblemDetailsFactoryTests
{
    private CustomProblemDetailsFactory _factory;

    [TestInitialize]
    public void SetUp()
    {
        var options = new ApiBehaviorOptions();
        var optionsWrapper = Options.Create(options);
        _factory = new CustomProblemDetailsFactory(optionsWrapper);

        options.ClientErrorMapping.Add(500, new ClientErrorData { Title = "internalError", Link = "testLink" });
    }

    [TestMethod]
    public void TestCreateProblemDetails_WithDefaults_ReturnsProblemDetails()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();

        // Act
        var problemDetails = _factory.CreateProblemDetails(httpContext);

        // Assert
        problemDetails.Should().NotBeNull();
        problemDetails.Status.Should().Be(500);
    }

    [TestMethod]
    public void TestCreateProblemDetails_WithStatusCode_ReturnsProblemDetails()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();

        // Act
        var problemDetails = _factory.CreateProblemDetails(httpContext, statusCode: StatusCodes.Status201Created);

        // Assert
        problemDetails.Should().NotBeNull();
        problemDetails.Status.Should().Be(StatusCodes.Status201Created);
    }

    [TestMethod]
    public void TestCreateValidationProblemDetails_WithDefaults_ReturnsValidationProblemDetails()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        var modelStateDictionary = new ModelStateDictionary();

        // Act
        var validationProblemDetails = _factory.CreateValidationProblemDetails(httpContext, modelStateDictionary);

        // Assert
        validationProblemDetails.Should().NotBeNull();
        validationProblemDetails.Status.Should().Be(400);
    }

    [TestMethod]
    public void TestCreateValidationProblemDetails_WithModelStateDictionaryNull_ThrowsNewArgumentNullException()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        ModelStateDictionary modelStateDictionary = null;

        // Act
        Func<Task> createValidation = async () => _factory.CreateValidationProblemDetails(httpContext, modelStateDictionary);

        // Assert
        createValidation.Should().ThrowAsync<ArgumentNullException>();
    }
}