using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using TaskPlanner.Application;

namespace TaskPlanner.Api.Infrastructure;

public sealed class ApiExceptionFilter : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        if (context.Exception is ServiceValidationException validationException)
        {
            context.Result = new BadRequestObjectResult(new ProblemDetails
            {
                Title = "Business validation failed",
                Detail = validationException.Message,
                Status = StatusCodes.Status400BadRequest
            });
            context.ExceptionHandled = true;
            return;
        }

        if (context.Exception is ServiceForbiddenException forbiddenException)
        {
            context.Result = new ObjectResult(new ProblemDetails
            {
                Title = "Forbidden",
                Detail = forbiddenException.Message,
                Status = StatusCodes.Status403Forbidden
            })
            {
                StatusCode = StatusCodes.Status403Forbidden
            };
            context.ExceptionHandled = true;
            return;
        }

        if (context.Exception is ServiceNotFoundException notFoundException)
        {
            context.Result = new NotFoundObjectResult(new ProblemDetails
            {
                Title = "Resource not found",
                Detail = notFoundException.Message,
                Status = StatusCodes.Status404NotFound
            });
            context.ExceptionHandled = true;
        }
    }
}
