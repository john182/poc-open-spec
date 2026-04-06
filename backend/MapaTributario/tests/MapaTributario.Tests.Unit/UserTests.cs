using MapaTributario.API.Domain.Entities;
using Shouldly;

namespace MapaTributario.Tests.Unit;

public class UserTests
{
    private User CriarUsuarioValido()
    {
        return User.Create("teste@teste.com", "Nome Original", "hash-original");
    }

    [Fact]
    public void AtualizarNome_ComNomeValido_DeveAtualizarNome()
    {
        var usuario = CriarUsuarioValido();

        usuario.AtualizarNome("Novo Nome");

        usuario.Nome.ShouldBe("Novo Nome");
    }

    [Fact]
    public void AtualizarNome_ComNomeVazio_DeveLancarExcecao()
    {
        var usuario = CriarUsuarioValido();

        var excecao = Should.Throw<ArgumentException>(() => usuario.AtualizarNome(""));

        excecao.ParamName.ShouldBe("nome");
    }

    [Fact]
    public void AtualizarNome_ComNomeNulo_DeveLancarExcecao()
    {
        var usuario = CriarUsuarioValido();

        var excecao = Should.Throw<ArgumentException>(() => usuario.AtualizarNome(null!));

        excecao.ParamName.ShouldBe("nome");
    }

    [Fact]
    public void AtualizarNome_ComEspacosEmBranco_DeveLancarExcecao()
    {
        var usuario = CriarUsuarioValido();

        var excecao = Should.Throw<ArgumentException>(() => usuario.AtualizarNome("   "));

        excecao.ParamName.ShouldBe("nome");
    }

    [Fact]
    public void AtualizarSenha_ComHashValido_DeveAtualizarPasswordHash()
    {
        var usuario = CriarUsuarioValido();

        usuario.AtualizarSenha("novo-hash");

        usuario.PasswordHash.ShouldBe("novo-hash");
    }

    [Fact]
    public void AtualizarSenha_ComHashVazio_DeveLancarExcecao()
    {
        var usuario = CriarUsuarioValido();

        var excecao = Should.Throw<ArgumentException>(() => usuario.AtualizarSenha(""));

        excecao.ParamName.ShouldBe("novoHash");
    }

    [Fact]
    public void AtualizarSenha_ComHashNulo_DeveLancarExcecao()
    {
        var usuario = CriarUsuarioValido();

        var excecao = Should.Throw<ArgumentException>(() => usuario.AtualizarSenha(null!));

        excecao.ParamName.ShouldBe("novoHash");
    }
}
