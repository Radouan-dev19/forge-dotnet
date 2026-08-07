using ForgeApiLab.Security;
using ForgeApiLab.Services;
using Microsoft.AspNetCore.Authentication;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddProblemDetails();
builder.Services.AddControllers();
builder.Services.AddSingleton<OrderStore>();
builder.Services
    .AddAuthentication(ApiKeyAuthenticationHandler.AuthenticationSchemeName)
    .AddScheme<AuthenticationSchemeOptions, ApiKeyAuthenticationHandler>(ApiKeyAuthenticationHandler.AuthenticationSchemeName, _ => { });
builder.Services.AddAuthorization(options =>
    options.AddPolicy("OrdersWrite", policy => policy.RequireRole("Operator")));

var app = builder.Build();
app.UseExceptionHandler();
app.UseAuthentication();
app.UseAuthorization();
app.MapGet("/health", () => Results.Text("Healthy")).AllowAnonymous();
app.MapControllers();
app.Run();

public partial class Program;
