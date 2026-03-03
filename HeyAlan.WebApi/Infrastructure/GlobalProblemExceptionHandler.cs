namespace HeyAlan.WebApi.Infrastructure;

using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

public sealed class GlobalProblemExceptionHandler : IExceptionHandler
{
    private readonly IProblemDetailsService problemDetailsService;
    private readonly ILogger<GlobalProblemExceptionHandler> logger;

    public GlobalProblemExceptionHandler(
        IProblemDetailsService problemDetailsService,
        ILogger<GlobalProblemExceptionHandler> logger)
    {
        this.problemDetailsService = problemDetailsService ?? throw new ArgumentNullException(nameof(problemDetailsService));
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (httpContext.Response.HasStarted)
        {
            return false;
        }

        ProblemDetails problemDetails;

        if (exception is BadHttpRequestException badRequestException)
        {
            problemDetails = new ProblemDetails
            {
                Status = badRequestException.StatusCode,
                Title = "Bad Request",
                Detail = string.IsNullOrWhiteSpace(badRequestException.Message)
                    ? "The request is invalid."
                    : badRequestException.Message,
                Type = $"https://httpstatuses.com/{badRequestException.StatusCode}",
            };
        }
        else
        {
            this.logger.LogError(exception, "Unhandled exception while processing request {Path}", httpContext.Request.Path);

            problemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "Internal Server Error",
                Detail = "An unexpected error occurred.",
                Type = "https://httpstatuses.com/500",
            };
        }

        httpContext.Response.StatusCode = problemDetails.Status ?? StatusCodes.Status500InternalServerError;

        bool written = await this.problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = problemDetails
        });

        if (!written)
        {
            await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);
        }

        return true;
    }
}
