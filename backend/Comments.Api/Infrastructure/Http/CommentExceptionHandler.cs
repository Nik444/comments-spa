using Comments.Api.Application.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Comments.Api.Infrastructure.Http;

public sealed class CommentExceptionHandler(IProblemDetailsService problemDetailsService) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        ProblemDetails? problem = exception switch
        {
            ParentNotFoundException e => new ValidationProblemDetails(
                new Dictionary<string, string[]> { ["ParentId"] = [e.Message] })
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Ошибки в данных запроса."
            },
            InvalidCaptchaException e => new ValidationProblemDetails(
                new Dictionary<string, string[]> { ["CaptchaAnswer"] = [e.Message] })
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Ошибки в данных запроса."
            },
            TooManyIdsException e => new ValidationProblemDetails(
                new Dictionary<string, string[]> { ["ids"] = [e.Message] })
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Ошибки в данных запроса."
            },
            ReplyLimitExceededException e => new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "В ветке достигнут предел ответов.",
                Detail = e.Message
            },
            _ => null
        };

        if (problem is null)
        {
            return false;
        }

        httpContext.Response.StatusCode = problem.Status!.Value;

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problem,
            Exception = exception
        });
    }
}
