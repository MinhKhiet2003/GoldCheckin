using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace Base.Common;

public static class JwtValidationGuard
{
    public static async Task Run(TokenValidatedContext context, Func<Task> validate)
    {
        try
        {
            await validate();
        }
        catch (OperationCanceledException) when (context.HttpContext.RequestAborted.IsCancellationRequested)
        {
            // A disconnected browser no longer needs a database lookup or an authentication response.
            context.NoResult();
            context.Response.StatusCode = 499;
        }
    }
}
