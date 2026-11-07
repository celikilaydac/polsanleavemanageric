using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PolsanIzin.Izinler.Veri;

namespace PolsanIzin.Izinler;

/// <summary>
/// Modülün iki public girişi (docs/architecture.md, ADR 0003). Talep oluşturma, `Takvim` ve
/// `Bakiyeler` modüllerinin public servislerine ve kayıtlı bir <see cref="TimeProvider"/>'a ihtiyaç duyar;
/// onları kaydetmek composition root'un (Web) işidir.
/// </summary>
public static class IzinlerModulu
{
    public static void Kaydet(IServiceCollection services, string baglantiDizesi)
    {
        services.AddDbContext<IzinlerDbContext>(o =>
            o.UseSqlite(baglantiDizesi, s => s.MigrationsHistoryTable(IzinlerDbContext.GecmisTablosu)));
        services.AddScoped<IIzinTalebiServisi, IzinTalebiServisi>();
    }

    public static void SemaKur(IServiceProvider services, bool demoVeriYukle)
    {
        // Izinler'in demo verisi yoktur (demo talepler kapsam dışı; ADR 0003 revisit tetikleyicisi).
        using var kapsam = services.CreateScope();
        kapsam.ServiceProvider.GetRequiredService<IzinlerDbContext>().Database.Migrate();
    }
}
