using System.Net;
using System.Text.Json;
using CompanyManager.Domain.Exceptions;

namespace CompanyManager.API.Middleware;

public class ExceptionMiddleware
{
    private static readonly JsonSerializerOptions _jsonOpts =
        new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    private readonly RequestDelegate  _next;
    private readonly ILogger<ExceptionMiddleware> _logger;
    private readonly IHostEnvironment _env;

    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger, IHostEnvironment env)
    {
        _next   = next;
        _logger = logger;
        _env    = env;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (DomainException ex) when (IsNotFound(ex.Code))
        {
            await WriteAsync(context, HttpStatusCode.NotFound, ex.Code, ex.Message);
        }
        catch (DomainException ex)
        {
            await WriteAsync(context, HttpStatusCode.BadRequest, ex.Code, ex.Message);
        }
        catch (UnauthorizedAccessException ex)
        {
            var msg = string.IsNullOrWhiteSpace(ex.Message)
                ? "Sem permissão para acessar este recurso."
                : ex.Message;
            await WriteAsync(context, HttpStatusCode.Forbidden, "forbidden", msg);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro inesperado: {Type} — {Message}", ex.GetType().Name, ex.Message);

            // Em desenvolvimento, devolve a mensagem real para facilitar o debug
            var message = _env.IsDevelopment()
                ? $"[{ex.GetType().Name}] {ex.Message}"
                : "Erro interno do servidor. Tente novamente mais tarde.";

            await WriteAsync(context, HttpStatusCode.InternalServerError, "internal_error", message);
        }
    }

    // ── Helpers ────────────────────────────────────────────────
    private static bool IsNotFound(string code) => code is
        "user_not_found"        or
        "department_not_found"  or
        "item_not_found"        or
        "technician_not_found"  or
        "schedule_not_found";

    private static Task WriteAsync(HttpContext ctx, HttpStatusCode status, string error, string message)
    {
        ctx.Response.ContentType = "application/json";
        ctx.Response.StatusCode  = (int)status;
        return ctx.Response.WriteAsync(
            JsonSerializer.Serialize(new { error, message }, _jsonOpts));
    }
}
