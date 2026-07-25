using MediatR;
using Microsoft.EntityFrameworkCore;
using Senkora.Application.Common.Interfaces;
using Senkora.Application.Common.Models;
using Senkora.Domain.Interfaces.Services;

namespace Senkora.Application.Features.Integration.Logo.Commands;

public sealed class UpdateLogoConnectionCommandHandler(
    IApplicationDbContext db,
    IEncryptionService encryption)
    : IRequestHandler<UpdateLogoConnectionCommand, Result>
{
    public async Task<Result> Handle(UpdateLogoConnectionCommand request, CancellationToken ct)
    {
        var conn = await db.LogoConnections.FirstOrDefaultAsync(
            c => c.Id == request.Id && c.TenantId == request.TenantId, ct);

        if (conn is null)
            return Result.Failure("Baglanti bulunamadi.", "NOT_FOUND");

        conn.Name          = request.Name;
        conn.RestUrl       = request.RestUrl.TrimEnd('/');
        conn.FirmNo        = request.FirmNo;
        conn.PeriodNo      = request.PeriodNo;
        conn.TimeoutSeconds= request.TimeoutSeconds;
        conn.IsActive      = request.IsActive;

        // Sifre/secret degistiyse guncelle
        if (!string.IsNullOrWhiteSpace(request.ClientId))
            conn.ClientIdEncrypted = encryption.Encrypt(request.ClientId);
        if (!string.IsNullOrWhiteSpace(request.ClientSecret))
            conn.ClientSecretEncrypted = encryption.Encrypt(request.ClientSecret);
        if (!string.IsNullOrWhiteSpace(request.Password))
            conn.PasswordEncrypted = encryption.Encrypt(request.Password);

        // Token cache'i temizle
        conn.CachedTokenEncrypted = null;
        conn.TokenExpiresAt       = null;
        conn.IsVerified           = false;

        await db.SaveChangesAsync(ct);
        return Result.Success();
    }
}
