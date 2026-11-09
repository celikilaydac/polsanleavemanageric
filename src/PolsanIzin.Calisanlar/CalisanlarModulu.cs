using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PolsanIzin.Calisanlar.Veri;

namespace PolsanIzin.Calisanlar;

/// <summary>Modülün iki public girişi (docs/architecture.md, ADR 0003).</summary>
public static class CalisanlarModulu
{
    public static void Kaydet(IServiceCollection services, string baglantiDizesi)
    {
        services.AddDbContext<CalisanlarDbContext>(o =>
            o.UseSqlite(baglantiDizesi, s => s.MigrationsHistoryTable(CalisanlarDbContext.GecmisTablosu)));
        services.AddSingleton<IPasswordHasher<Calisan>, PasswordHasher<Calisan>>();
        services.AddScoped<IKimlikDogrulamaServisi, KimlikDogrulamaServisi>();
        services.AddScoped<ICalisanRehberi, CalisanRehberi>();
    }

    public static void SemaKur(IServiceProvider services, bool demoVeriYukle)
    {
        using var kapsam = services.CreateScope();
        var db = kapsam.ServiceProvider.GetRequiredService<CalisanlarDbContext>();
        db.Database.Migrate();

        if (demoVeriYukle)
        {
            DemoVeriYukle(db, kapsam.ServiceProvider.GetRequiredService<IPasswordHasher<Calisan>>());
        }
    }

    private static void DemoVeriYukle(CalisanlarDbContext db, IPasswordHasher<Calisan> parolaHasher)
    {
        var mevcut = db.Calisanlar.Select(c => c.Id).ToHashSet();

        // Yöneticiler önce: yabancı anahtar (YoneticiId) eklenme sırasına bağlı.
        foreach (var demo in DemoVeri.Calisanlar.Where(d => !mevcut.Contains(d.Id)))
        {
            var calisan = new Calisan
            {
                Id = demo.Id,
                AdSoyad = demo.AdSoyad,
                Eposta = EpostaNormalize.Normalize(demo.Eposta),
                ParolaHash = string.Empty,
                YoneticiId = demo.YoneticiId,
                Roller = [.. demo.Roller.Select(r => new CalisanRolu { CalisanId = demo.Id, Rol = r })],
            };
            calisan.ParolaHash = parolaHasher.HashPassword(calisan, DemoVeri.Parola);
            db.Calisanlar.Add(calisan);
            db.SaveChanges();
        }
    }
}
