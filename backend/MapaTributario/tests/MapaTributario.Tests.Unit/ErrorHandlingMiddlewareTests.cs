using System.Net;
using MapaTributario.API.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using Shouldly;

namespace MapaTributario.Tests.Unit;

public class ErrorHandlingMiddlewareTests
{
    private readonly Mock<ILogger<ErrorHandlingMiddleware>> _logger = new();

    [Fact]
    public async Task InvokeAsync_SemExcecao_PassaAdiante()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        bool nextCalled = false;
        var middleware = new ErrorHandlingMiddleware(_ => { nextCalled = true; return Task.CompletedTask; }, _logger.Object);

        await middleware.InvokeAsync(context);

        nextCalled.ShouldBeTrue();
    }

    [Fact]
    public async Task InvokeAsync_ComExcecao_Retorna500()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        var middleware = new ErrorHandlingMiddleware(_ => throw new Exception("boom"), _logger.Object);

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.ShouldBe((int)HttpStatusCode.InternalServerError);
    }
}
