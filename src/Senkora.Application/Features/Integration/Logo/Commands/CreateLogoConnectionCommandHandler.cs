using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Senkora.Application.Common.Interfaces;
using Senkora.Application.Common.Models;
using Senkora.Domain.Entities.Integration;
using Senkora.Domain.Interfaces.Services;

namespace Senkora.Application.Features.Integration.Logo.Commands;

public sealed class CreateLogoConnectionCommandHandler(
    IApplicationDbContext db,
    IEncryptionService encryption,
    ILogger<CreateLogoConnectionCommandHandler> logger)
    : IRequestHandler<CreateLogoConnectionCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(
        CreateLogoConnectionCommand request, CancellationToken ct)
    {
        // Ayni tenant icin ayni URL ile baska aktif baglanti var mi?
        var exists = await db.LogoConnections.AnyAsync(
            c => c.TenantId == request.TenantId &&
                 c.RestUrl  == request.RestUrl  &&
                 c.FirmNo   == request.FirmNo   &&
                 !c.IsDeleted, ct);

        if (exists)
            return Result<Guid>.Failure(
                "Bu URL ve firma numarasi ile zaten bir baglanti tanimli.", "DUPLICATE_CONNECTION");

        var connection = new LogoConnection
        {
            TenantId               = request.TenantId,
            Name                   = request.Name,
            RestUrl                = request.RestUrl.TrimEnd('/'),
            ClientIdEncrypted      = encryption.Encrypt(request.ClientId),
            ClientSecretEncrypted  = encryption.Encrypt(request.ClientSecret),
            Username               = request.Username,
            PasswordEncrypted      = encryption.Encrypt(request.Password),
            FirmNo                 = request.FirmNo,
            PeriodNo               = request.PeriodNo,
            TimeoutSeconds         = request.TimeoutSeconds,
            IsActive               = true,
            IsVerified             = false,
            CreatedBy              = request.TenantId.ToString()
        };

        db.LogoConnections.Add(connection);
        await db.SaveChangesAsync(ct);

        logger.LogInformation(
            "LogoConnection created: {Id} for tenant {TenantId}", connection.Id, request.TenantId);

        return Result<Guid>.Success(connection.Id);
    }
}
