using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using PolsanIzin.Calisanlar.Veri;

namespace PolsanIzin.Calisanlar.Tests;

/// <summary>Paylaşılan adlandırılmış in-memory SQLite üzerinde kurulmuş Calisanlar modülü.</summary>
internal sealed class CalisanlarOrtami : IDisposable
{
    private readonly SqliteConnection _canliTut;
    private readonly ServiceProvider _servisler;

    public CalisanlarOrtami(bool demoVeriYukle = true)
    {
        var baglanti = $"Data Source=calisanlar-{Guid.NewGuid()};Mode=Memory;Cache=Shared";
        _canliTut = new SqliteConnection(baglanti);
        _canliTut.Open();

        var services = new ServiceCollection();
        CalisanlarModulu.Kaydet(services, baglanti);
        _servisler = services.BuildServiceProvider();
        CalisanlarModulu.SemaKur(_servisler, demoVeriYukle);
    }

    public IServiceProvider Servisler => _servisler;

    public T Kullan<T>(Func<IServiceProvider, T> islem)
    {
        using var kapsam = _servisler.CreateScope();
        return islem(kapsam.ServiceProvider);
    }

    public List<Calisan> TumCalisanlar() => Kullan(sp =>
    {
        var db = sp.GetRequiredService<CalisanlarDbContext>();
        return db.Calisanlar.Select(c => new Calisan
        {
            Id = c.Id,
            AdSoyad = c.AdSoyad,
            Eposta = c.Eposta,
            ParolaHash = c.ParolaHash,
            YoneticiId = c.YoneticiId,
            Roller = c.Roller.ToList(),
        }).ToList();
    });

    public async Task<Ortak.Sonuc<OturumBilgisi>> DogrulaAsync(string eposta, string parola)
    {
        using var kapsam = _servisler.CreateScope();
        return await kapsam.ServiceProvider.GetRequiredService<IKimlikDogrulamaServisi>()
            .DogrulaAsync(eposta, parola, TestContext.Current.CancellationToken);
    }

    public void Dispose()
    {
        _servisler.Dispose();
        _canliTut.Dispose();
    }
}
