namespace Api.Shared.Exceptions;

public sealed class ValidationDomainException(string code, string message) : DomainException(code, message);
