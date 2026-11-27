namespace CRM.Api.Security;

public static class CrmSessionCookies
{
    public const string AccessName = "crm_access";
    public const string RefreshName = "crm_refresh";
    public const string RefreshPath = "/api/auth/session";
    public const string RequestHeader = "X-CRM-Request";

    public static void Set(HttpContext context, IWebHostEnvironment environment, string accessToken, string refreshToken)
    {
        context.Response.Headers.CacheControl = "no-store";
        context.Response.Cookies.Append(AccessName, accessToken, Options(context, environment, "/api", TimeSpan.FromMinutes(15)));
        context.Response.Cookies.Append(RefreshName, refreshToken, Options(context, environment, RefreshPath, TimeSpan.FromDays(7)));
    }

    public static void Clear(HttpContext context, IWebHostEnvironment environment)
    {
        context.Response.Headers.CacheControl = "no-store";
        context.Response.Cookies.Delete(AccessName, Options(context, environment, "/api", null));
        context.Response.Cookies.Delete(RefreshName, Options(context, environment, RefreshPath, null));
    }

    private static CookieOptions Options(HttpContext context, IWebHostEnvironment environment, string path, TimeSpan? maxAge) => new()
    {
        HttpOnly = true,
        Secure = !environment.IsDevelopment() || context.Request.IsHttps,
        SameSite = SameSiteMode.Strict,
        Path = path,
        MaxAge = maxAge,
        IsEssential = true
    };
}
