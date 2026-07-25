using Senkora.Domain.Enums;

namespace Senkora.Domain.Interfaces.Services;

public interface ILicenseValidator
{
    Task<bool> ValidateAsync(Guid tenantId, CancellationToken ct = default);
    Task<bool> HasFeatureAsync(Guid tenantId, LicenseTier minimumTier, CancellationToken ct = default);
    Task<bool> CanAddConnectionAsync(Guid tenantId, ConnectorType type, CancellationToken ct = default);
}
