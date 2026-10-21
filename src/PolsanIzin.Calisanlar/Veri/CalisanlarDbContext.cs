using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace PolsanIzin.Calisanlar.Veri;

internal sealed class CalisanlarDbContext(DbContextOptions<CalisanlarDbContext> options) : DbContext(options)
{
    public const string GecmisTablosu = "__EFMigrationsHistory_Calisanlar";

    public DbSet<Calisan> Calisanlar => Set<Calisan>();

    public DbSet<CalisanRolu> CalisanRolleri => Set<CalisanRolu>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<DateTimeOffset>().HaveConversion<UtcTicksDonusturucu>();
        configurationBuilder.Properties<Enum>().HaveConversion<string>();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Calisan>(e =>
        {
            e.ToTable("Calisanlar");
            e.HasKey(c => c.Id);
            e.Property(c => c.AdSoyad).HasMaxLength(200);
            e.Property(c => c.Eposta).HasMaxLength(256);
            e.HasIndex(c => c.Eposta).IsUnique();
            e.HasOne<Calisan>().WithMany().HasForeignKey(c => c.YoneticiId).OnDelete(DeleteBehavior.Restrict);
            e.HasMany(c => c.Roller).WithOne().HasForeignKey(r => r.CalisanId);
        });

        modelBuilder.Entity<CalisanRolu>(e =>
        {
            e.ToTable("CalisanRolleri");
            e.HasKey(r => new { r.CalisanId, r.Rol });
            e.Property(r => r.Rol).HasMaxLength(20);
        });
    }
}

/// <summary>Yalnızca <c>dotnet ef migrations</c> için (tasarım zamanı).</summary>
internal sealed class CalisanlarDbContextTasarimFabrikasi : IDesignTimeDbContextFactory<CalisanlarDbContext>
{
    public CalisanlarDbContext CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<CalisanlarDbContext>()
            .UseSqlite("Data Source=tasarim.db", o => o.MigrationsHistoryTable(CalisanlarDbContext.GecmisTablosu))
            .Options);
}
