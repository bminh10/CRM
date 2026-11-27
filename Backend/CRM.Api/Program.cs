using Microsoft.EntityFrameworkCore;
using CRM.Api.Data;
using CRM.Api.Business.Interface;
using CRM.Api.Business.Services;
using CRM.Api.Security;
using Microsoft.AspNetCore.Authentication;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddScoped<ICompanyService, CompanyService>();
builder.Services.AddSingleton<CrmTokenService>();
builder.Services.AddAuthentication("Bearer")
    .AddScheme<AuthenticationSchemeOptions, CrmBearerHandler>("Bearer", _ => { });
builder.Services.AddAuthorization();
builder.Services.AddControllers();

//Kết nối tới DB

builder.Services.AddDbContext<CrmDbContext>(options=>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"))
);

// Đăng ký dịch vụ Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();
app.Use(async (context, next) =>
{
    var method = context.Request.Method;
    var unsafeMethod = !HttpMethods.IsGet(method) && !HttpMethods.IsHead(method)
        && !HttpMethods.IsOptions(method);
    if (unsafeMethod
        && (context.Request.Cookies.ContainsKey(CrmSessionCookies.AccessName)
            || context.Request.Path.StartsWithSegments("/api/auth/session"))
        && context.Request.Headers[CrmSessionCookies.RequestHeader] != "1")
    {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        await context.Response.WriteAsJsonAsync(new { message = "Missing session request header." });
        return;
    }
    await next();
});
app.UseAuthentication();
app.UseAuthorization();
// Cấu hình Swagger UI
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();
app.MapGet("/api/health", () => Results.Ok(new { status = "Healthy" }))
    .WithName("HealthCheck");
app.MapGet("/api/health/live", () => Results.Ok(new { status = "Live" }))
    .WithName("LivenessCheck");
app.MapGet("/api/health/ready", async (CrmDbContext db) =>
{
    var isConnected = await db.Database.CanConnectAsync();
    return isConnected
        ? Results.Ok(new { status = "Ready" })
        : Results.Problem(title: "Not ready", detail: "Database connection is unavailable.", statusCode: 503);
}).WithName("ReadinessCheck");
app.MapGet("/api/health/database", async (CrmDbContext db) =>
{
    bool isConnected = await db.Database.CanConnectAsync();

    if (isConnected)
    {
        return Results.Ok(new
        {
            status = "Connected",
            message = "Success"
        });
    }

    return Results.Problem(
        title: "Error",
        detail: "Khong the ket noi toi DB!"
    );
}).WithName("CheckDatabaseConnection");


app.Run();