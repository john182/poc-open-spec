using System.Text;
using FluentValidation;
using MapaTributario.API.Application.Auth;
using MapaTributario.API.Domain.Interfaces;
using MapaTributario.API.Infrastructure.Auth;
using MapaTributario.API.Infrastructure.Repository;
using MapaTributario.API.Infrastructure.Repository.Mongo;
using MapaTributario.API.Middleware;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Driver;

var builder = WebApplication.CreateBuilder(args);

// MongoDB
MongoMappings.Register();
var mongoUri = builder.Configuration["MONGO_URI"]
    ?? builder.Configuration.GetConnectionString("MongoDB")
    ?? "mongodb://localhost:27017";
var mongoDb = builder.Configuration["MONGO_DB"] ?? "mapa_tributario";
builder.Services.AddSingleton<IMongoDatabase>(
    new MongoClient(mongoUri).GetDatabase(mongoDb));

// Domain interfaces -> Infrastructure
builder.Services.AddSingleton<IUserRepository, UserRepository>();
builder.Services.AddSingleton<IPasswordHasher, BcryptPasswordHasher>();
builder.Services.AddSingleton<ITokenProvider, JwtTokenProvider>();

// Application
builder.Services.AddScoped<RegisterUser>();
builder.Services.AddScoped<LoginUser>();
builder.Services.AddScoped<RefreshToken>();

// FluentValidation
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

// JWT
var jwtSecret = builder.Configuration["JWT:Secret"]
    ?? builder.Configuration["JWT_SECRET"]
    ?? "default-dev-secret-change-in-production-32chars";
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["JWT:Issuer"] ?? "MapaTributario",
            ValidateAudience = true,
            ValidAudience = builder.Configuration["JWT:Audience"] ?? "MapaTributario",
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });
builder.Services.AddAuthorization();

// Controllers + OpenAPI
builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

app.UseMiddleware<ErrorHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

public partial class Program { }
