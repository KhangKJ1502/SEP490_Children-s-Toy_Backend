using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ToyStore.Infrastructure.Data;

namespace ToyStore.API.Middleware;

public class AccountStatusGuardMiddleware
{
    private const string AccountLockedCode = "ACCOUNT_LOCKED";

    private readonly RequestDelegate _next;
    private readonly ILogger<AccountStatusGuardMiddleware> _logger;

    public AccountStatusGuardMiddleware(
        RequestDelegate next,
        ILogger<AccountStatusGuardMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, SEP490ToyStoreContext dbContext)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var accountIdClaim = context.User.Claims.FirstOrDefault(c => c.Type == "accountId")?.Value;

            if (int.TryParse(accountIdClaim, out var accountId) && accountId > 0)
            {
                var accountStatus = await dbContext.Accounts
                    .AsNoTracking()
                    .Where(account => account.AccountId == accountId)
                    .Select(account => new
                    {
                        account.IsActive,
                        account.IsDeleted
                    })
                    .FirstOrDefaultAsync(context.RequestAborted);

                if (accountStatus is null || accountStatus.IsDeleted || !accountStatus.IsActive)
                {
                    _logger.LogInformation(
                        "Rejected request from inactive or deleted account {AccountId} on {Path}.",
                        accountId,
                        context.Request.Path);

                    await WriteAccountLockedResponseAsync(context);
                    return;
                }
            }
        }

        await _next(context);
    }

    private static async Task WriteAccountLockedResponseAsync(HttpContext context)
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        context.Response.ContentType = "application/json";

        var result = JsonSerializer.Serialize(new
        {
            success = false,
            code = AccountLockedCode,
            errorCode = AccountLockedCode,
            message = "Your account has been locked. Please contact support for assistance."
        });

        await context.Response.WriteAsync(result, context.RequestAborted);
    }
}

public static class AccountStatusGuardMiddlewareExtensions
{
    public static IApplicationBuilder UseAccountStatusGuard(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<AccountStatusGuardMiddleware>();
    }
}
