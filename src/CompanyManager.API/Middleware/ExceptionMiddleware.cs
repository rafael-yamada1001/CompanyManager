using System.Net;
using System.Text.Json;
using CompanyManager.Domain.Exceptions;

namespace CompanyManager.API.Middleware;

public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;

    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (DomainException ex) when (ex.Code is "user_not_found" or "department_not_found" or "item_not_found" or "person_not_found")
        {
            await WriteErrorAsync(context, HttpStatusCode.NotFound, ex.Code, ex.Message);
        }
        catch (DomainException ex)
        {
            await WriteErrorAsync(context, HttpStatusCode.BadRequest, ex.Code, ex.Message);
        }
        catch (UnauthorizedAccessException)
        {
            await WriteErrorAsync(context, HttpStatusCode.Forbidden, "forbidden", "Sem permissão para acessar este recurso.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro inesperado.");
            await WriteErrorAsync(context, HttpStatusCode.InternalServerError, "internal_error", "Erro interno do servidor.");
        }
    }

    private static Task WriteErrorAsync(HttpContext context, HttpStatusCode status, string error, string message)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)status;

        var body = JsonSerializer.Serialize(new { error, message });
        return context.Response.WriteAsync(body);
    }
}
