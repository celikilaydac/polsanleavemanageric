using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace PolsanIzin.Bakiyeler.Veri;

internal sealed class BakiyelerDbContext(DbContextOptions<BakiyelerDbContext> options) : DbContext(options)
{
    public const string GecmisTablosu = "__EFMigrationsHistory_Bakiyeler";

    public DbSet<IzinHakki> IzinHaklari => Set<IzinHakki>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<DateTimeOffset>().HaveConversion<UtcTicksDonusturucu>();
        configurationBuilder.Properties<Enum>().HaveConversion<string>();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<IzinHakki>(e =>
        {
            e.ToTable("IzinHaklari");
            e.HasKey(h => new { h.CalisanId, h.Yil });
        });
    }
}

/// <summary>docs/conventions.md: <see cref="DateTimeOffset"/> UTC ticks (<c>long</c>) olarak saklanır.</summary>
internal sealed class UtcTicksDonusturucu()
    : ValueConverter<DateTimeOffset, long>(v => v.UtcTicks, v => new DateTimeOffset(v, TimeSpan.Zero));

/// <summary>Yalnızca <c>dotnet ef migrations</c> için (tasarım zamanı).</summary>
internal sealed class BakiyelerDbContextTasarimFabrikasi : IDesignTimeDbContextFactory<BakiyelerDbContext>
{
    public BakiyelerDbContext CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<BakiyelerDbContext>()
            .UseSqlite("Data Source=tasarim.db", o => o.MigrationsHistoryTable(BakiyelerDbContext.GecmisTablosu))
            .Options);
}
