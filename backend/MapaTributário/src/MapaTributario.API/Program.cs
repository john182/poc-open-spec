using MapaTributario.API.Extensions;
using MapaTributario.API.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Infrastructure layer: MongoDB, repositories, auth infra, seeds, NFS-e client
builder.Services.AddMapaTributarioInfrastructure(builder.Configuration);

// Application layer: use cases, services, resilience, JWT auth, validation
builder.Services.AddMapaTributarioApplication(builder.Configuration);

// Controllers + OpenAPI
builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

// MongoDB indexes (idempotent — safe to run on every startup)
await app.ApplyMongoIndexesAsync();

// Seed data (idempotent)
await app.RunSeedsAsync();

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
