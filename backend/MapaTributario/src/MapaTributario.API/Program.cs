using MapaTributario.API.Extensions;
using MapaTributario.API.Middleware;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

// Infrastructure layer: MongoDB, repositories, auth infra, seeds, NFS-e client
builder.Services.AddMapaTributarioInfrastructure(builder.Configuration);

// Application layer: use cases, services, resilience, JWT auth, validation
builder.Services.AddMapaTributarioApplication(builder.Configuration);

// Controllers + Swagger
builder.Services.AddControllers();
builder.Services.AddSwaggerGen(opcoes =>
{
    opcoes.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "MapaTributario API",
        Version = "v1",
        Description = "API do Mapa Tributário — consulta de alíquotas NFS-e por município"
    });

    opcoes.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Cole apenas o token JWT (sem 'Bearer ' na frente).",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });

    opcoes.AddSecurityRequirement(documento =>
    {
        var requisito = new OpenApiSecurityRequirement();
        var referencia = new OpenApiSecuritySchemeReference("Bearer", documento);
        requisito.Add(referencia, new List<string>());
        return requisito;
    });
});

var app = builder.Build();

// MongoDB indexes (idempotent — safe to run on every startup)
await app.ApplyMongoIndexesAsync();

// Seed data (idempotent)
await app.RunSeedsAsync();

app.UseMiddleware<ErrorHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(opcoes =>
    {
        opcoes.SwaggerEndpoint("/swagger/v1/swagger.json", "MapaTributario API v1");
        opcoes.EnableTryItOutByDefault();
    });
    app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();
}

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

public partial class Program { }
