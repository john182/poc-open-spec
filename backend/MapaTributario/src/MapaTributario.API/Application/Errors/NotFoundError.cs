using FluentResults;

namespace MapaTributario.API.Application.Errors;

public class NotFoundError : Error
{
    public NotFoundError(string message) : base(message) { }
}
