using FluentResults;

namespace MapaTributario.API.Application.Errors;

public class ValidationError : Error
{
    public ValidationError(string message) : base(message) { }
}
