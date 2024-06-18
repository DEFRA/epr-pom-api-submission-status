namespace EPR.SubmissionMicroservice.Data;

using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Net;
using Microsoft.Azure.Cosmos;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

/// <summary>
///     This class is written based on CosmosExecutionStrategy.cs
///     in namespace Microsoft.EntityFrameworkCore.Cosmos.Storage.Internal.
/// </summary>
[ExcludeFromCodeCoverage]
public class CosmosDbRetryExecutionStrategy : ExecutionStrategy
{
    public CosmosDbRetryExecutionStrategy(
        DbContext context)
        : this(context, DefaultMaxRetryCount)
    {
    }

    public CosmosDbRetryExecutionStrategy(
        ExecutionStrategyDependencies dependencies)
        : this(dependencies, DefaultMaxRetryCount)
    {
    }

    public CosmosDbRetryExecutionStrategy(
        DbContext context,
        int maxRetryCount)
        : this(context, maxRetryCount, DefaultMaxDelay)
    {
    }

    public CosmosDbRetryExecutionStrategy(
        ExecutionStrategyDependencies dependencies,
        int maxRetryCount)
        : this(dependencies, maxRetryCount, DefaultMaxDelay)
    {
    }

    public CosmosDbRetryExecutionStrategy(DbContext context, int maxRetryCount, TimeSpan maxRetryDelay)
        : base(context, maxRetryCount, maxRetryDelay)
    {
    }

    public CosmosDbRetryExecutionStrategy(
        ExecutionStrategyDependencies dependencies,
        int maxRetryCount,
        TimeSpan maxRetryDelay)
        : base(dependencies, maxRetryCount, maxRetryDelay)
    {
    }

    protected override bool ShouldRetryOn(Exception exception)
    {
        return exception switch
        {
            CosmosException cosmosException => IsTransient(cosmosException.StatusCode),
            WebException webException => IsTransient(((HttpWebResponse)webException.Response!).StatusCode),
            { } => IsTransient(GetExceptionResponse(exception).StatusCode),
            _ => false
        };

        static bool IsTransient(HttpStatusCode statusCode)
            => statusCode == HttpStatusCode.ServiceUnavailable
               || statusCode == HttpStatusCode.TooManyRequests;
    }

    protected override TimeSpan? GetNextDelay(Exception lastException)
    {
        var baseDelay = base.GetNextDelay(lastException);
        return baseDelay == null
            ? null
            : CallOnWrappedException(lastException, GetDelayFromException)
              ?? baseDelay;
    }

    private static TimeSpan? GetDelayFromException(Exception exception)
    {
        switch (exception)
        {
            case CosmosException cosmosException:
                return cosmosException.RetryAfter;

            case WebException webException:
            {
                var response = (HttpWebResponse)webException.Response!;

                var delayString = response.Headers.GetValues("x-ms-retry-after-ms")?.FirstOrDefault();
                if (TryParseMsRetryAfter(delayString, out var delay))
                {
                    return delay;
                }

                delayString = response.Headers.GetValues("Retry-After")?.FirstOrDefault();
                if (TryParseRetryAfter(delayString, out delay))
                {
                    return delay;
                }

                return null;
            }

            case { } httpException:
            {
                var response = GetExceptionResponse(httpException);
                if (response.Headers.TryGetValues("x-ms-retry-after-ms", out var values)
                    && TryParseMsRetryAfter(values.FirstOrDefault(), out var delay))
                {
                    return delay;
                }

                if (response.Headers.TryGetValues("Retry-After", out values)
                    && TryParseRetryAfter(values.FirstOrDefault(), out delay))
                {
                    return delay;
                }

                return null;
            }

            default:
                return null;
        }

        static bool TryParseMsRetryAfter(string? delayString, out TimeSpan delay)
        {
            delay = default;
            if (delayString == null)
            {
                return false;
            }

            if (int.TryParse(delayString, out var intDelay))
            {
                delay = TimeSpan.FromMilliseconds(intDelay);
                return true;
            }

            return false;
        }

        static bool TryParseRetryAfter(string? delayString, out TimeSpan delay)
        {
            delay = default;
            if (delayString == null)
            {
                return false;
            }

            if (int.TryParse(delayString, out var intDelay))
            {
                delay = TimeSpan.FromSeconds(intDelay);
                return true;
            }

            if (DateTimeOffset.TryParse(delayString, CultureInfo.InvariantCulture, DateTimeStyles.None, out var retryDate))
            {
                delay = retryDate.Subtract(DateTimeOffset.Now);
                delay = delay <= TimeSpan.Zero ? TimeSpan.FromMilliseconds(1) : delay;
                return true;
            }

            return false;
        }
    }

    private static HttpResponseMessage GetExceptionResponse(Exception exception) =>
        exception.GetType().GetProperty(nameof(WebException.Response)).GetValue(exception) as HttpResponseMessage;
}
