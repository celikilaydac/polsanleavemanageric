using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PolsanIzin.Takvim.Veri;

namespace PolsanIzin.Takvim;

/// <summary>Modülün iki public girişi (docs/architecture.md, ADR 0003).</summary>
public static class TakvimModulu
{
    public static void Kaydet(IServiceCollection services, string baglantiDizesi)
    {
        services.AddDbContext<TakvimDbContext>(o =>
            o.UseSqlite(baglantiDizesi, s => s.MigrationsHistoryTable(TakvimDbContext.GecmisTablosu)));
        services.AddScoped<ITakvimServisi, TakvimServisi>();
    }

    public static void SemaKur(IServiceProvider services, bool demoVeriYukle)
    {
        using var kapsam = services.CreateScope();
        var db = kapsam.ServiceProvider.GetRequiredService<TakvimDbContext>();
        db.Database.Migrate();

        if (demoVeriYukle)
        {
            var mevcut = db.ResmiTatiller.Select(t => t.Tarih).ToHashSet();
            db.ResmiTatiller.AddRange(DemoVeri.Tatiller
                .Where(t => !mevcut.Contains(t.Tarih))
                .Select(t => new ResmiTatil { Tarih = t.Tarih, Ad = t.Ad }));
            db.SaveChanges();
        }
    }
}
