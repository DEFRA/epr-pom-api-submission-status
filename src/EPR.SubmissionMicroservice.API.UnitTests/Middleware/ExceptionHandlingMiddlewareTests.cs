namespace EPR.SubmissionMicroservice.API.UnitTests.Middleware;

using API.Middleware;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;

[TestClass]
public class ExceptionHandlingMiddlewareTests
{
    private Mock<RequestDelegate> _nextMock;
    private Mock<ILogger<ExceptionHandlingMiddleware>> _loggerMock;
    private ExceptionHandlingMiddleware _middleware;

    [TestInitialize]
    public void SetUp()
    {
        _nextMock = new Mock<RequestDelegate>();
        _loggerMock = new Mock<ILogger<ExceptionHandlingMiddleware>>();
        _middleware = new ExceptionHandlingMiddleware(_nextMock.Object, _loggerMock.Object);
    }

    [TestMethod]
    public async Task TestInvokeAsync_WhenNoException_CallsNextDelegate()
    {
        // Arrange
        var context = new DefaultHttpContext();

        // Act
        await _middleware.InvokeAsync(context);

        // Assert
        _nextMock.Verify(next => next(context), Times.Once);
    }

    [TestMethod]
    public async Task TestInvokeAsync_WhenExceptionOccurs_Returns500InternalServerError()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var exception = new Exception("Test exception");

        _nextMock.Setup(next => next(context)).ThrowsAsync(exception);

        // Act
        await _middleware.InvokeAsync(context);

        // Assert
        context.Response.ContentType.Should().Be("application/json");
        context.Response.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
    }
}