using System.Text.Json;

namespace Fiscal.Api.Middleware;

public sealed class GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Exceção não tratada em {Path}", context.Request.Path);
            await EscreverErro(context, ex);
        }
    }

    private static async Task EscreverErro(HttpContext context, Exception ex)
    {
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/json";

        var response = JsonSerializer.Serialize(new
        {
            erro = "Erro interno do servidor",
            mensagem = ex.Message,
            traceId = context.TraceIdentifier
        });

        await context.Response.WriteAsync(response);
    }
}
