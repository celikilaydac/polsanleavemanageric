using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace PolsanIzin.Izinler.Veri;

internal sealed class IzinlerDbContext(DbContextOptions<IzinlerDbContext> options) : DbContext(options)
{
    public const string GecmisTablosu = "__EFMigrationsHistory_Izinler";

    public DbSet<IzinTalebi> IzinTalepleri => Set<IzinTalebi>();

    public DbSet<DenetimKaydi> DenetimKayitlari => Set<DenetimKaydi>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<DateTimeOffset>().HaveConversion<UtcTicksDonusturucu>();
        configurationBuilder.Properties<Enum>().HaveConversion<string>();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<IzinTalebi>(e =>
        {
            e.ToTable("IzinTalepleri");
            e.HasKey(t => t.Id);
            e.HasIndex(t => new { t.CalisanId, t.Durum });
            e.Property(t => t.IzinTuru).HasMaxLength(20);
            e.Property(t => t.Durum).HasMaxLength(20);
            e.Property(t => t.Aciklama).HasMaxLength(500);
        });

        modelBuilder.Entity<DenetimKaydi>(e =>
        {
            e.ToTable("DenetimKayitlari");
            e.HasKey(d => d.Id);
            e.HasIndex(d => d.TalepId);
            e.Property(d => d.EskiDurum).HasMaxLength(20);
            e.Property(d => d.YeniDurum).HasMaxLength(20);
        });
    }
}

/// <summary>docs/conventions.md: <see cref="DateTimeOffset"/> UTC ticks (<c>long</c>) olarak saklanır.</summary>
internal sealed class UtcTicksDonusturucu()
    : ValueConverter<DateTimeOffset, long>(v => v.UtcTicks, v => new DateTimeOffset(v, TimeSpan.Zero));

/// <summary>Yalnızca <c>dotnet ef migrations</c> için (tasarım zamanı).</summary>
internal sealed class IzinlerDbContextTasarimFabrikasi : IDesignTimeDbContextFactory<IzinlerDbContext>
{
    public IzinlerDbContext CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<IzinlerDbContext>()
            .UseSqlite("Data Source=tasarim.db", o => o.MigrationsHistoryTable(IzinlerDbContext.GecmisTablosu))
            .Options);
}
