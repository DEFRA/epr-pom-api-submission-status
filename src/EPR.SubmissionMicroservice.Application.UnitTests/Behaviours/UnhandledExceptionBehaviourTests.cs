namespace EPR.SubmissionMicroservice.Application.UnitTests.Behaviours;

using Application.Behaviours;
using ErrorOr;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;

[TestClass]
public class UnhandledExceptionBehaviourTests
{
    private Mock<ILogger<IRequest<IErrorOr>>> _loggerMock;

    [TestInitialize]
    public void SetUp()
    {
        _loggerMock = new Mock<ILogger<IRequest<IErrorOr>>>();
    }

    [TestMethod]
    public async Task TestHandle_WhenExceptionThrown_ShouldLogErrorAndRethrow()
    {
        // Arrange
        var request = new Mock<IRequest<IErrorOr>>();
        var cancellationToken = CancellationToken.None;
        var requestHandlerDelegate = new Mock<RequestHandlerDelegate<IErrorOr>>();
        var exception = new InvalidOperationException("Test exception");
        requestHandlerDelegate.Setup(x => x()).ThrowsAsync(exception);

        var unhandledExceptionBehaviour = new UnhandledExceptionBehaviour<IRequest<IErrorOr>, IErrorOr>(_loggerMock.Object);

        // Act
        Func<Task> action = async () => await unhandledExceptionBehaviour.Handle(request.Object, requestHandlerDelegate.Object, cancellationToken);

        // Assert
        await action.Should().ThrowAsync<InvalidOperationException>().WithMessage("Test exception");

        _loggerMock.Verify(
            x => x.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Unhandled Exception for Request")),
            exception,
            It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }
}