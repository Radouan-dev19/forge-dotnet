using System.Text;
using ForgeJwtLab.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// La clé de signature vient de la configuration locale : une valeur factice, jamais un secret
// réel dans le dépôt. En production, elle viendrait d'un coffre, hors du code et hors de Git.
string signingKey = builder.Configuration["Jwt:SigningKey"]
    ?? throw new InvalidOperationException("Configuration Jwt:SigningKey absente.");
string issuer = builder.Configuration["Jwt:Issuer"]
    ?? throw new InvalidOperationException("Configuration Jwt:Issuer absente.");
string audience = builder.Configuration["Jwt:Audience"]
    ?? throw new InvalidOperationException("Configuration Jwt:Audience absente.");

builder.Services.AddControllers();

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // Chaque propriété ci-dessous correspond à une étape de la chaîne de validation
        // travaillée à la main dans les exercices de la semaine 14. Le middleware ne fait
        // rien de magique : il exécute cette chaîne à chaque requête.
        options.TokenValidationParameters = new TokenValidationParameters
        {
            // L'algorithme est imposé par le serveur, jamais lu dans l'en-tête du jeton.
            ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
            ValidateIssuerSigningKey = true,

            // Émetteur et audience sont obligatoires : sans eux, rejeu croisé possible.
            ValidateIssuer = true,
            ValidIssuer = issuer,
            ValidateAudience = true,
            ValidAudience = audience,

            // Expiration exigée, avec une tolérance d'horloge courte et explicite.
            RequireExpirationTime = true,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30),
        };
    });

builder.Services.AddAuthorization(options =>
{
    // Une politique par portée : le contrôleur déclare ce qu'il exige, la politique
    // décrit comment la revendication scope — liste séparée par des espaces — se lit.
    options.AddPolicy(ScopeRequirement.ReadOrders, policy =>
        policy.RequireAuthenticatedUser()
            .RequireAssertion(context => ScopeRequirement.HasScope(context.User, ScopeRequirement.ReadOrders)));
    options.AddPolicy(ScopeRequirement.WriteOrders, policy =>
        policy.RequireAuthenticatedUser()
            .RequireAssertion(context => ScopeRequirement.HasScope(context.User, ScopeRequirement.WriteOrders)));
});

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

// La sonde de vie reste anonyme : un orchestrateur n'a pas de jeton.
app.MapGet("/health", () => Results.Ok(new { status = "healthy" })).AllowAnonymous();

app.MapControllers();

app.Run();

/// <summary>Point d'entrée exposé pour que la suite de tests monte l'API en mémoire.</summary>
public partial class Program;
