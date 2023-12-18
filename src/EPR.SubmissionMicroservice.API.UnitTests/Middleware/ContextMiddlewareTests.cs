namespace EPR.SubmissionMicroservice.API.UnitTests.Middleware;

using API.Middleware;
using API.Services;
using API.Services.Interfaces;
using Application.Models;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;

[TestClass]
public class ContextMiddlewareTests
{
    private Mock<RequestDelegate> _nextMock;
    private Mock<ILogger<ContextMiddleware>> _loggerMock;
    private Mock<IHeaderParser> _headerParserMock;
    private ContextMiddleware _middleware;

    [TestInitialize]
    public void SetUp()
    {
        _nextMock = new Mock<RequestDelegate>();
        _loggerMock = new Mock<ILogger<ContextMiddleware>>();
        _headerParserMock = new Mock<IHeaderParser>();
        _middleware = new ContextMiddleware(_nextMock.Object, _loggerMock.Object, _headerParserMock.Object);
    }

    [TestMethod]
    public async Task TestInvokeAsync_WhenHeaderIsValid_CallsNextDelegate()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var userId = Guid.NewGuid();
        var orgId = Guid.NewGuid();
        var userContextProvider = new UserContextProvider()
        {
            EmailAddress = "email",
        };
        var header = new PomHeader(orgId, userId);

        _headerParserMock.Setup(h => h.Parse(context.Request.Headers)).Returns(header);

        // Act
        await _middleware.InvokeAsync(context, userContextProvider);

        // Assert
        userContextProvider.UserId.Should().Be(userId);
        userContextProvider.OrganisationId.Should().Be(orgId);
        _nextMock.Verify(next => next(context), Times.Once);
    }

    [TestMethod]
    public async Task TestInvokeAsync_WhenHeaderIsNull_Returns403Forbidden()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var userContextProvider = new UserContextProvider()
        {
            EmailAddress = "email",
        };
        _headerParserMock.Setup(h => h.Parse(context.Request.Headers)).Returns((PomHeader)null);

        // Act
        await _middleware.InvokeAsync(context, userContextProvider);

        // Assert
        _nextMock.Verify(next => next(context), Times.Never);
        context.Response.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
    }
}