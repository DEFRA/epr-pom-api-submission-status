namespace EPR.SubmissionMicroservice.API.Middleware;

using Application.Interfaces;
using Services.Interfaces;

public class ContextMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IHeaderParser _headerParser;
    private readonly ILogger<ContextMiddleware> _logger;

    public ContextMiddleware(
        RequestDelegate next,
        ILogger<ContextMiddleware> logger,
        IHeaderParser headerParser)
    {
        _next = next;
        _logger = logger;
        _headerParser = headerParser;
    }

    public async Task InvokeAsync(HttpContext context, IUserContextProvider userContextProvider)
    {
        var header = _headerParser.Parse(context.Request.Headers);

        if (header != null)
        {
            userContextProvider.SetContext(header.UserId, header.OrganisationId);
            await _next.Invoke(context);
        }
        else
        {
            _logger.LogWarning("Headers is not valid");
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            context.Response.WriteAsync("Unauthorized");
        }
    }
}