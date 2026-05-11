using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using BioLicense_Portal.Application.Extensions;
using BioLicense_Portal.Application.Exceptions;
using BioLicense_Portal.Application.Common.Constants;
using System.Collections.Generic;

namespace BioLicense_Portal.WebAPI.Middleware
{
    public class CustomExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<CustomExceptionMiddleware> _logger;
        private readonly IHostEnvironment _env;

        public CustomExceptionMiddleware(
            RequestDelegate next,
            ILogger<CustomExceptionMiddleware> logger,
            IHostEnvironment env)
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
            catch (Exception ex)
            {
                await HandleExceptionAsync(context, ex);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            var response = context.Response;
            response.ContentType = "application/json";

            object result;
            int statusCode;

            switch (exception)
            {
                case NotFoundException ex:
                    statusCode = 404;
                    result = ApiResponse.NotFound(ex.Message);
                    _logger.LogWarning(ex, "Resource not found: {Message}", ex.Message);
                    break;

                case BusinessException ex:
                    statusCode = 400;
                    result = ApiResponse.BadRequest(ex.Message);
                    _logger.LogWarning(ex, "Business rule violation: {Message}", ex.Message);
                    break;

                case ValidationException ex:
                    statusCode = 400;
                    result = ApiResponse.BadRequest(ex.Message, ex.Errors);
                    _logger.LogWarning(ex, "Validation failed: {Message}", ex.Message);
                    break;

                case UnauthorizedException ex:
                    statusCode = 401;
                    result = ApiResponse.Unauthorized(ex.Message);
                    _logger.LogWarning(ex, "Unauthorized: {Message}", ex.Message);
                    break;

                case UnauthorizedAccessException ex:
                    statusCode = 403;
                    result = ApiResponse.Forbidden(Messages.Auth.Forbidden);
                    _logger.LogWarning(ex, "Forbidden access");
                    break;

                case KeyNotFoundException ex:
                    statusCode = 404;
                    result = ApiResponse.NotFound(ex.Message);
                    _logger.LogWarning(ex, "Key not found");
                    break;

                case ArgumentNullException ex:
                    statusCode = 400;
                    result = ApiResponse.BadRequest($"Parameter '{ex.ParamName}' is required");
                    _logger.LogWarning(ex, "Null argument");
                    break;

                case ArgumentException ex:
                    statusCode = 400;
                    result = ApiResponse.BadRequest(ex.Message);
                    _logger.LogWarning(ex, "Invalid argument: {Message}", ex.Message);
                    break;

                case InvalidOperationException ex:
                    statusCode = 400;
                    result = ApiResponse.BadRequest(ex.Message);
                    _logger.LogWarning(ex, "Invalid operation: {Message}", ex.Message);
                    break;

                case DbUpdateException ex:
                    statusCode = 400;
                    var dbMessage = _env.IsProduction() ? Messages.General.InternalError : 
                        (ex.InnerException?.InnerException?.Message ?? ex.InnerException?.Message ?? ex.Message);

                    result = ApiResponse.BadRequest(dbMessage);
                    _logger.LogError(ex, "Database error");
                    break;

                case JsonException ex:
                    statusCode = 400;
                    var jsonMessage = _env.IsProduction() ? Messages.General.BadRequest : ex.Message;
                    result = ApiResponse.BadRequest(jsonMessage);
                    _logger.LogWarning(ex, "JSON Deserialization failed: {Message}", ex.Message);
                    break;

                default:
                    statusCode = 500;
                    var message = _env.IsProduction() ? Messages.General.InternalError : exception.Message;
                    result = ApiResponse.InternalError(message);
                    _logger.LogError(exception, "Unhandled exception");
                    break;
            }

            response.StatusCode = statusCode;
            await response.WriteAsync(JsonSerializer.Serialize(result, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            }));
        }
    }
}
