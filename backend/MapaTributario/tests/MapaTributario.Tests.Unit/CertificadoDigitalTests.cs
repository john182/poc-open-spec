using MapaTributario.API.Domain.Entities;
using Shouldly;

namespace MapaTributario.Tests.Unit;

public class CertificadoDigitalTests
{
    [Fact]
    public void Criar_DevePreencherTodasAsPropriedades()
    {
        // Arrange
        byte[] pfxBytes = [0x01, 0x02, 0x03];
        string senha = "senha-teste";
        string thumbprint = "ABC123DEF456";
        string subject = "CN=Teste, O=Empresa";
        DateTime validoAte = new(2027, 12, 31, 23, 59, 59, DateTimeKind.Utc);
        DateTime antesDaCriacao = DateTime.UtcNow;

        // Act
        CertificadoDigital resultado = CertificadoDigital.Criar(pfxBytes, senha, thumbprint, subject, validoAte);

        // Assert
        resultado.PfxBytes.ShouldBe(pfxBytes);
        resultado.Senha.ShouldBe(senha);
        resultado.Thumbprint.ShouldBe(thumbprint);
        resultado.Subject.ShouldBe(subject);
        resultado.ValidoAte.ShouldBe(validoAte);
        resultado.DataUpload.ShouldBeGreaterThanOrEqualTo(antesDaCriacao);
        resultado.DataUpload.ShouldBeLessThanOrEqualTo(DateTime.UtcNow);
    }

    [Fact]
    public void Criar_DeveDefinirDataUploadComoUtcNow()
    {
        // Arrange
        DateTime antesDaCriacao = DateTime.UtcNow;

        // Act
        CertificadoDigital resultado = CertificadoDigital.Criar(
            [0x01], "senha", "thumb", "CN=Teste", DateTime.UtcNow.AddYears(1));

        // Assert
        resultado.DataUpload.Kind.ShouldBe(DateTimeKind.Utc);
        resultado.DataUpload.ShouldBeGreaterThanOrEqualTo(antesDaCriacao);
    }

    [Fact]
    public void Criar_DeveDefinirIdComoIdFixo()
    {
        // Arrange & Act
        CertificadoDigital resultado = CertificadoDigital.Criar(
            [0x01], "senha", "thumb", "CN=Teste", DateTime.UtcNow.AddYears(1));

        // Assert — Id fixo para garantir upsert atômico (documento singleton)
        resultado.Id.ShouldBe(CertificadoDigital.IdFixo);
    }

    [Fact]
    public void Criar_ComDadosDiferentes_DeveRetornarInstanciasDiferentes()
    {
        // Arrange & Act
        CertificadoDigital cert1 = CertificadoDigital.Criar(
            [0x01], "senha1", "thumb1", "CN=Cert1", DateTime.UtcNow.AddYears(1));
        CertificadoDigital cert2 = CertificadoDigital.Criar(
            [0x02], "senha2", "thumb2", "CN=Cert2", DateTime.UtcNow.AddYears(2));

        // Assert
        cert1.ShouldNotBeSameAs(cert2);
        cert1.Thumbprint.ShouldNotBe(cert2.Thumbprint);
        cert1.Subject.ShouldNotBe(cert2.Subject);
    }
}
