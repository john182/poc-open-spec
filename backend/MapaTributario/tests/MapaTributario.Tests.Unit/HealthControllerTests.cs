#pragma warning disable CS8604 // ToString() em objetos anônimos retorna string? mas nunca é null nos testes
using MapaTributario.API.Controllers;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;
using Moq;
using Shouldly;

namespace MapaTributario.Tests.Unit;

public class HealthControllerTests
{
    private readonly Mock<IMongoDatabase> _database = new();

    private HealthController CriarSut()
    {
        return new HealthController(_database.Object);
    }

    [Fact]
    public async Task Given_MongoConectado_Should_RetornarOkHealthy()
    {
        // Arrange
        HealthController sut = CriarSut();
        _database.Setup(d => d.RunCommandAsync<BsonDocument>(
                It.IsAny<Command<BsonDocument>>(),
                It.IsAny<ReadPreference>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BsonDocument("ok", 1));

        // Act
        IActionResult resultado = await sut.Get();

        // Assert
        OkObjectResult ok = resultado.ShouldBeOfType<OkObjectResult>();
        ok.Value!.ToString().ShouldContain("healthy");
    }

    [Fact]
    public async Task Given_MongoDesconectado_Should_Retornar503Unhealthy()
    {
        // Arrange
        HealthController sut = CriarSut();
        _database.Setup(d => d.RunCommandAsync<BsonDocument>(
                It.IsAny<Command<BsonDocument>>(),
                It.IsAny<ReadPreference>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TimeoutException("Conexão com MongoDB expirou"));

        // Act
        IActionResult resultado = await sut.Get();

        // Assert
        ObjectResult objectResult = resultado.ShouldBeOfType<ObjectResult>();
        objectResult.StatusCode.ShouldBe(503);
        objectResult.Value!.ToString().ShouldContain("unhealthy");
    }
}
