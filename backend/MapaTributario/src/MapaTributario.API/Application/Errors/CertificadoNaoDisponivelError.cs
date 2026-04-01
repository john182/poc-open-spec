using FluentResults;

namespace MapaTributario.API.Application.Errors;

public class CertificadoNaoDisponivelError : Error
{
    public CertificadoNaoDisponivelError()
        : base("Nenhum certificado digital disponível. Faça upload do certificado PFX ou configure o caminho em NfseApi:CertificatePath") { }
}
