namespace Senkora.Application.Common.Interfaces;

public interface ICurrentTenant
{
    Guid TenantId { get; }
    string Subdomain { get; }
    bool IsResolved { get; }
}
