namespace EPR.SubmissionMicroservice.API.UnitTests.Controllers;

using API.Controllers;
using Data.Constants;
using ErrorOr;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

[TestClass]
public class ApiControllerTests : ApiController
{
    public ApiControllerTests()
    {
        ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
    }

    [TestMethod]
    [DataRow(StatusCodes.Status401Unauthorized)]
    [DataRow(StatusCodes.Status404NotFound)]
    [DataRow(StatusCodes.Status409Conflict)]
    [DataRow(StatusCodes.Status500InternalServerError)]
    public void Problem_ReturnCorrectStatusCode(int statusCode)
    {
        // Act
        var error = GetError(statusCode);
        var result = Problem(new List<Error> { error }) as ObjectResult;

        // Assert
        result.Should().NotBeNull();
        result.StatusCode.Should().Be(statusCode);
        result.Value.As<ProblemDetails>().Detail.Should().Be(error.Type.ToString());
    }

    private static Error GetError(int errorCode) => errorCode switch
    {
        StatusCodes.Status401Unauthorized => Error.Custom(CustomErrorType.Unauthorized, CustomErrorCode.OrganisationUnauthorized, string.Empty),
        StatusCodes.Status404NotFound => Error.NotFound(),
        StatusCodes.Status409Conflict => Error.Conflict(),
        StatusCodes.Status500InternalServerError => Error.Failure(),
    };
}