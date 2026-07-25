using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Senkora.Infrastructure.Persistence;

/// <summary>
/// Design-time factory — sadece "dotnet ef" komutu tarafından kullanılır.
/// </summary>
public sealed class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    // Gelistirme ortami — SQL Server adi: AHK
    private const string DevConnectionString =
        "Server=AHK,1433;Database=SenkoraDb;User Id=sa;Password=LOGO;TrustServerCertificate=True;";

    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        optionsBuilder.UseSqlServer(DevConnectionString);
        return new ApplicationDbContext(optionsBuilder.Options);
    }
}
