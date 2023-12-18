namespace EPR.SubmissionMicroservice.Application.UnitTests.Behaviours;

using Application.Behaviours;
using ErrorOr;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;

[TestClass]
public class PerformanceBehaviourTests
{
    private Mock<ILogger<IRequest<IErrorOr>>> _loggerMock;

    [TestInitialize]
    public void SetUp()
    {
        _loggerMock = new Mock<ILogger<IRequest<IErrorOr>>>();
    }

    [TestMethod]
    public async Task TestHandle_WhenCalled_ShouldLogWarningIfLongRunning()
    {
        // Arrange
        var request = new Mock<IRequest<IErrorOr>>();
        var cancellationToken = CancellationToken.None;
        var requestHandlerDelegate = new Mock<RequestHandlerDelegate<IErrorOr>>();
        requestHandlerDelegate.Setup(x => x()).ReturnsAsync(Mock.Of<IErrorOr>());

        var performanceBehaviour = new PerformanceBehaviour<IRequest<IErrorOr>, IErrorOr>(_loggerMock.Object);

        // Act
        var response = await performanceBehaviour.Handle(request.Object, requestHandlerDelegate.Object, cancellationToken);

        // Assert
        response.Should().NotBeNull();

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Long Running Request")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Never);
    }
}
