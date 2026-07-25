namespace Senkora.Domain.Exceptions;

public class LicenseException : DomainException
{
    public LicenseException(string message)
        : base("LICENSE_VIOLATION", message) { }
}
