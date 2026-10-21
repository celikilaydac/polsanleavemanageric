using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace PolsanIzin.Takvim.Veri;

internal sealed class TakvimDbContext(DbContextOptions<TakvimDbContext> options) : DbContext(options)
{
    public const string GecmisTablosu = "__EFMigrationsHistory_Takvim";

    public DbSet<ResmiTatil> ResmiTatiller => Set<ResmiTatil>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<DateTimeOffset>().HaveConversion<UtcTicksDonusturucu>();
        configurationBuilder.Properties<Enum>().HaveConversion<string>();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ResmiTatil>(e =>
        {
            e.ToTable("ResmiTatiller");
            e.HasKey(t => t.Tarih);
            e.Property(t => t.Ad).HasMaxLength(100);
        });
    }
}

/// <summary>docs/conventions.md: <see cref="DateTimeOffset"/> UTC ticks (<c>long</c>) olarak saklanır.</summary>
internal sealed class UtcTicksDonusturucu()
    : ValueConverter<DateTimeOffset, long>(v => v.UtcTicks, v => new DateTimeOffset(v, TimeSpan.Zero));

/// <summary>Yalnızca <c>dotnet ef migrations</c> için (tasarım zamanı).</summary>
internal sealed class TakvimDbContextTasarimFabrikasi : IDesignTimeDbContextFactory<TakvimDbContext>
{
    public TakvimDbContext CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<TakvimDbContext>()
            .UseSqlite("Data Source=tasarim.db", o => o.MigrationsHistoryTable(TakvimDbContext.GecmisTablosu))
            .Options);
}
