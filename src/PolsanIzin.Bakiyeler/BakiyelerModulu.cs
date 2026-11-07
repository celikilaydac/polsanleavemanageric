using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PolsanIzin.Bakiyeler.Veri;

namespace PolsanIzin.Bakiyeler;

/// <summary>Modülün iki public girişi (docs/architecture.md, ADR 0003).</summary>
public static class BakiyelerModulu
{
    public static void Kaydet(IServiceCollection services, string baglantiDizesi)
    {
        services.AddDbContext<BakiyelerDbContext>(o =>
            o.UseSqlite(baglantiDizesi, s => s.MigrationsHistoryTable(BakiyelerDbContext.GecmisTablosu)));
        services.AddScoped<IBakiyeServisi, BakiyeServisi>();
    }

    public static void SemaKur(IServiceProvider services, bool demoVeriYukle)
    {
        using var kapsam = services.CreateScope();
        var db = kapsam.ServiceProvider.GetRequiredService<BakiyelerDbContext>();
        db.Database.Migrate();

        if (demoVeriYukle)
        {
            var mevcut = db.IzinHaklari.Where(h => h.Yil == DemoVeri.Yil).Select(h => h.CalisanId).ToHashSet();
            db.IzinHaklari.AddRange(DemoVeri.CalisanKimlikleri
                .Where(id => !mevcut.Contains(id))
                .Select(id => new IzinHakki { CalisanId = id, Yil = DemoVeri.Yil, Gun = DemoVeri.Gun }));
            db.SaveChanges();
        }
    }
}
