namespace Senkora.Domain.Exceptions;

public class SyncException : DomainException
{
    public SyncException(string code, string message)
        : base(code, message) { }
}
