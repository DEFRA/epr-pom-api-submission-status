namespace EPR.SubmissionMicroservice.Application.Behaviours;

using Data.Extensions;
using ErrorOr;
using MediatR;
using Microsoft.Extensions.Logging;

public class LoggingBehaviour<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
    where TResponse : IErrorOr
{
    private readonly ILogger<TRequest> _logger;

    public LoggingBehaviour(ILogger<TRequest> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        _logger.LogEnter(typeof(TRequest).Name);

        var response = await next();

        _logger.LogExit(typeof(TRequest).Name);

        return response;
    }
}
