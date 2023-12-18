namespace EPR.SubmissionMicroservice.Application.UnitTests.Behaviours;

using System.Linq.Expressions;
using Application.Behaviours;
using ErrorOr;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Moq;

[TestClass]
public class ValidationBehaviourTests
{
    private Mock<IValidator<IRequest<IErrorOr>>> _validatorMock;

    [TestInitialize]
    public void SetUp()
    {
        _validatorMock = new Mock<IValidator<IRequest<IErrorOr>>>();
    }

    [TestMethod]
    public async Task TestHandle_WhenValidationSucceeds_ShouldCallNext()
    {
        // Arrange
        var request = new Mock<IRequest<IErrorOr>>();
        var cancellationToken = CancellationToken.None;
        var requestHandlerDelegate = new Mock<RequestHandlerDelegate<IErrorOr>>();
        var response = new Mock<IErrorOr>();

        requestHandlerDelegate.Setup(expectedDelegate => expectedDelegate()).ReturnsAsync(response.Object);
        _validatorMock.Setup(x => x.ValidateAsync(request.Object, cancellationToken))
            .ReturnsAsync(new ValidationResult());

        var validationBehaviour = new ValidationBehaviour<IRequest<IErrorOr>, IErrorOr>(_validatorMock.Object);

        // Act
        var result = await validationBehaviour.Handle(request.Object, requestHandlerDelegate.Object, cancellationToken);

        // Assert
        requestHandlerDelegate.Verify(expectedDelegate => expectedDelegate(), Times.Once);
    }

    [TestMethod]
    public async Task TestHandle_WhenValidationFails_ShouldReturnErrorResponse()
    {
        // Arrange
        var request = new Mock<IRequest<IErrorOr>>();
        var cancellationToken = CancellationToken.None;
        var requestHandlerDelegate = new Mock<RequestHandlerDelegate<IErrorOr>>();
        Expression<Action<RequestHandlerDelegate<IErrorOr>>> requestHandler = x => x();

        var validationFailures = new List<ValidationFailure>
        {
            new ValidationFailure("TestProperty", "Test error message")
        };

        _validatorMock.Setup(x => x.ValidateAsync(request.Object, cancellationToken))
            .ReturnsAsync(new ValidationResult(validationFailures));

        var validationBehaviour = new ValidationBehaviour<IRequest<IErrorOr>, IErrorOr>(_validatorMock.Object);

        // Act
        Func<Task> act = async () => await validationBehaviour.Handle(request.Object, requestHandlerDelegate.Object, cancellationToken);

        // Assert
        await act.Should().ThrowAsync<ValidationException>();
        requestHandlerDelegate.Verify(requestHandler, Times.Never);
    }
}