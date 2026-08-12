using Hangfire.Dashboard;

namespace DevContentEngine.Worker.Hangfire;

public sealed class HangfireBasicAuthFilter : IDashboardAuthorizationFilter
{
    private readonly string _username;
    private readonly string _password;

    public HangfireBasicAuthFilter(string username, string password)
    {
        _username = username;
        _password = password;
    }

    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();

        if (BasicAuthCredentialChecker.Matches(httpContext.Request.Headers.Authorization, _username, _password))
        {
            return true;
        }

        httpContext.Response.Headers.WWWAuthenticate = "Basic realm=\"Hangfire Dashboard\"";
        httpContext.Response.StatusCode = StatusCodes.Status401Unauthorized;

        return false;
    }
}
