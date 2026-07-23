using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Text.Json;

namespace HotelPOS.Api.Middleware
{
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionMiddleware> _logger;
        private readonly IHostEnvironment _env;

        private static readonly JsonSerializerOptions SerializerOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

        public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger, IHostEnvironment env)
        {
            _next = next;
            _logger = logger;
            _env = env;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (FluentValidation.ValidationException valEx)
            {
                _logger.LogWarning(valEx, "Validation error occurred: {Message}", valEx.Message);
                var errors = valEx.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Select(e => e.ErrorMessage).ToArray()
                    );

                var response = new ValidationProblemDetails(errors)
                {
                    Status = 400,
                    Title = "One or more validation errors occurred.",
                    Detail = "Please refer to the errors property for additional details."
                };

                await WriteResponseAsync(context, HttpStatusCode.BadRequest, response);
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Resource not found: {Message}", ex.Message);
                // ex.Message here is always a message the application itself constructed (e.g.
                // "Order #5 not found."), never a raw framework/DB exception - safe to return as-is.
                var response = new ProblemDetails { Status = 404, Title = "Resource not found.", Detail = ex.Message }; // NOSONAR
                await WriteResponseAsync(context, HttpStatusCode.NotFound, response);
            }
            catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
            {
                _logger.LogWarning(ex, "Request error occurred: {Message}", ex.Message);
                // ex.Message here is always a message the application itself constructed (e.g.
                // "An item with the name 'X' already exists."), never a raw framework/DB
                // exception - safe to return as-is.
                var response = new ProblemDetails { Status = 400, Title = "The request could not be processed.", Detail = ex.Message }; // NOSONAR
                await WriteResponseAsync(context, HttpStatusCode.BadRequest, response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);

                var response = _env.IsDevelopment()
                    ? new ProblemDetails { Status = 500, Title = ex.Message, Detail = ex.StackTrace }
                    : new ProblemDetails { Status = 500, Title = "Internal Server Error", Detail = "An unexpected error occurred." };

                await WriteResponseAsync(context, HttpStatusCode.InternalServerError, response);
            }
        }

        private static async Task WriteResponseAsync(HttpContext context, HttpStatusCode statusCode, ProblemDetails response)
        {
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)statusCode;
            // Serialize via the runtime type so derived types like ValidationProblemDetails keep their extra properties (e.g. Errors).
            await context.Response.WriteAsync(JsonSerializer.Serialize(response, response.GetType(), SerializerOptions));
        }
    }
}
