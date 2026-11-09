using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PolsanIzin.Bakiyeler.Veri;
using PolsanIzin.Ortak;

namespace PolsanIzin.Bakiyeler.Tests;

/// <summary>Bakiye sorgusu, rezerve ve geri alma (spec 0001 AC-20…AC-23, BR-2, R-9).</summary>
public sealed class BakiyeServisiTests : IDisposable
{
    private static readonly Guid _calisan = Guid.CreateVersion7();

    private readonly SqliteConnection _canliTut;
    private readonly ServiceProvider _servisler;

    public BakiyeServisiTests()
    {
        var baglanti = $"Data Source=bakiyeler-{Guid.NewGuid()};Mode=Memory;Cache=Shared";
        _canliTut = new SqliteConnection(baglanti);
        _canliTut.Open();
        var services = new ServiceCollection();
        BakiyelerModulu.Kaydet(services, baglanti);
        _servisler = services.BuildServiceProvider();
        BakiyelerModulu.SemaKur(_servisler, demoVeriYukle: false);
        Kullan(db =>
        {
            db.IzinHaklari.Add(new IzinHakki { CalisanId = _calisan, Yil = 2026, Gun = 14 });
            db.SaveChanges();
        });
    }

    public void Dispose()
    {
        _servisler.Dispose();
        _canliTut.Dispose();
    }

    private static CancellationToken Iptal => TestContext.Current.CancellationToken;

    private void Kullan(Action<BakiyelerDbContext> islem)
    {
        using var kapsam = _servisler.CreateScope();
        islem(kapsam.ServiceProvider.GetRequiredService<BakiyelerDbContext>());
    }

    private async Task<T> ServisAsync<T>(Func<IBakiyeServisi, Task<T>> islem)
    {
        using var kapsam = _servisler.CreateScope();
        return await islem(kapsam.ServiceProvider.GetRequiredService<IBakiyeServisi>());
    }

    private int HareketSayisi()
    {
        using var kapsam = _servisler.CreateScope();
        return kapsam.ServiceProvider.GetRequiredService<BakiyelerDbContext>().BakiyeHareketleri.Count();
    }

    [Fact]
    public async Task KullanilabilirBakiye_HakTanimsiz_NullDoner()
    {
        Assert.Null(await ServisAsync(s => s.KullanilabilirBakiyeAsync(_calisan, 2027, Iptal)));
    }

    [Fact]
    public async Task KullanilabilirBakiye_KesinVeRezerve_HaktanDusulur()
    {
        Kullan(db =>
        {
            db.BakiyeHareketleri.Add(new BakiyeHareketi { Id = Guid.CreateVersion7(), TalepId = Guid.CreateVersion7(), CalisanId = _calisan, Yil = 2026, Gun = 3, Tur = HareketTuru.Kesin });
            db.BakiyeHareketleri.Add(new BakiyeHareketi { Id = Guid.CreateVersion7(), TalepId = Guid.CreateVersion7(), CalisanId = _calisan, Yil = 2026, Gun = 2, Tur = HareketTuru.Rezerve });
            db.SaveChanges();
        });

        Assert.Equal(9, await ServisAsync(s => s.KullanilabilirBakiyeAsync(_calisan, 2026, Iptal)));
    }

    [Fact]
    public async Task Rezerve_BakiyeKadar_Kabul()
    {
        var sonuc = await ServisAsync(s => s.RezerveAsync(_calisan, 2026, 14, Guid.CreateVersion7(), Iptal));

        Assert.True(sonuc.Basarili);
        Assert.Equal(0, await ServisAsync(s => s.KullanilabilirBakiyeAsync(_calisan, 2026, Iptal)));
    }

    [Fact]
    public async Task Rezerve_BakiyePlusBir_BR2Hatasi()
    {
        var sonuc = await ServisAsync(s => s.RezerveAsync(_calisan, 2026, 15, Guid.CreateVersion7(), Iptal));

        var hata = Assert.Single(sonuc.Hatalar);
        Assert.Equal("BR-2", hata.Kod);
        Assert.Equal(BakiyeMesajlari.Yetersiz(2026, 14, 15), hata.Mesaj);
        Assert.Equal(0, HareketSayisi());
    }

    [Fact]
    public async Task Rezerve_MevcutRezerveVarken_KalanaGoreKarar()
    {
        Assert.True((await ServisAsync(s => s.RezerveAsync(_calisan, 2026, 5, Guid.CreateVersion7(), Iptal))).Basarili);

        var on = await ServisAsync(s => s.RezerveAsync(_calisan, 2026, 10, Guid.CreateVersion7(), Iptal));
        var dokuz = await ServisAsync(s => s.RezerveAsync(_calisan, 2026, 9, Guid.CreateVersion7(), Iptal));

        Assert.Equal("BR-2", Assert.Single(on.Hatalar).Kod);
        Assert.True(dokuz.Basarili);
        Assert.Equal(0, await ServisAsync(s => s.KullanilabilirBakiyeAsync(_calisan, 2026, Iptal)));
    }

    [Fact]
    public async Task Rezerve_HakTanimsiz_BR2HakTanimsizMesaji()
    {
        var sonuc = await ServisAsync(s => s.RezerveAsync(_calisan, 2027, 1, Guid.CreateVersion7(), Iptal));

        var hata = Assert.Single(sonuc.Hatalar);
        Assert.Equal("BR-2", hata.Kod);
        Assert.Equal(BakiyeMesajlari.HakTanimsiz, hata.Mesaj);
        Assert.Equal(0, HareketSayisi());
    }

    [Fact]
    public async Task Rezerve_BakiyeYetersiz_HareketYazilmaz()
    {
        Assert.True((await ServisAsync(s => s.RezerveAsync(_calisan, 2026, 10, Guid.CreateVersion7(), Iptal))).Basarili);

        var sonuc = await ServisAsync(s => s.RezerveAsync(_calisan, 2026, 5, Guid.CreateVersion7(), Iptal));

        Assert.False(sonuc.Basarili);
        Assert.Equal(1, HareketSayisi());
        Assert.Equal(4, await ServisAsync(s => s.KullanilabilirBakiyeAsync(_calisan, 2026, Iptal)));
    }

    [Fact]
    public async Task Rezerve_EsZamanliIkiRezerve_BakiyeNegatifeDusmez()
    {
        // Her biri tek başına sığar (10 ≤ 14), ikisi birlikte sığmaz. SQLite'ın async çağrıları senkron
        // çalıştığından gerçek eşzamanlılık için iki iş parçacığı aynı anda başlatılır ve senaryo
        // tekrarlanır (review 0001 K-1).
        for (var tur = 0; tur < 20; tur++)
        {
            Kullan(db => db.BakiyeHareketleri.ExecuteDelete());
            using var basla = new ManualResetEventSlim();

            Task<Sonuc> RezerveEt() => Task.Run(() =>
            {
                basla.Wait(Iptal);
                return ServisAsync(s => s.RezerveAsync(_calisan, 2026, 10, Guid.CreateVersion7(), Iptal));
            }, Iptal);

            var gorevler = new[] { RezerveEt(), RezerveEt() };
            basla.Set();
            var sonuclar = await Task.WhenAll(gorevler);

            Assert.Single(sonuclar, s => s.Basarili);
            Assert.Equal(4, await ServisAsync(s => s.KullanilabilirBakiyeAsync(_calisan, 2026, Iptal)));
        }
    }

    [Fact]
    public async Task RezerveGeriAl_Rezerve_BakiyeGeriGelir()
    {
        var talepId = Guid.CreateVersion7();
        Assert.True((await ServisAsync(s => s.RezerveAsync(_calisan, 2026, 5, talepId, Iptal))).Basarili);

        var sonuc = await ServisAsync(s => s.RezerveGeriAlAsync(talepId, Iptal));

        Assert.True(sonuc.Basarili);
        Assert.Equal(14, await ServisAsync(s => s.KullanilabilirBakiyeAsync(_calisan, 2026, Iptal)));
        Assert.Equal(0, HareketSayisi());
    }
}
