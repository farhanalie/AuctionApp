using System;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using AuctionApp.Shared.Errors;
using AuctionApp.Shared.Exceptions;
using Microsoft.AspNetCore.Http;

namespace AuctionApp.Middleware
{
    public class ErrorHandlingMiddleware
    {
        private readonly RequestDelegate _next;

        public ErrorHandlingMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context /* other scoped dependencies */)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(context, ex);
            }
        }

        private static Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            var error = new ErrorResponse
            {
                Error = new Error
                {
                    StatusCode = (int)HttpStatusCode.InternalServerError,
                    Message = exception.Message 
                }
            };

            #if DEBUG
            if (exception.InnerException?.TargetSite is { })
                if (exception.TargetSite is { })
                    error.Error.Details = new[]
                    {
                        new ErrorDetail
                        {
                            Target = exception.TargetSite.ToString(),
                            Message = exception.StackTrace
                        },
                        new ErrorDetail
                        {
                            Target = exception.InnerException?.TargetSite.ToString(),
                            Message = exception.InnerException?.Message
                        }
                    };
#else
                            error.Error.Message = "Some Error Occured on server please try again";
            #endif

            switch (exception)
            {
                //case ForbiddenException _:
                //    error.Error.StatusCode = (int) HttpStatusCode.Forbidden;
                //    break;
                case BadRequestException _:
                    error.Error.StatusCode = (int)HttpStatusCode.BadRequest;
                    break;
            }

            var result = JsonSerializer.Serialize(error);
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = error.Error.StatusCode;
            return context.Response.WriteAsync(result);
        }
    }
}