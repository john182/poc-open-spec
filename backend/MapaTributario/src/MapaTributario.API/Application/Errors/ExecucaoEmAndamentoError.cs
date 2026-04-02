using FluentResults;

namespace MapaTributario.API.Application.Errors;

public class ExecucaoEmAndamentoError : Error
{
    public ExecucaoEmAndamentoError()
        : base("Uma execução do crawler já está em andamento") { }
}
