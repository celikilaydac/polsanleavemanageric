using PolsanIzin.Calisanlar.Veri;

namespace PolsanIzin.Calisanlar.Tests;

public sealed class DemoVeriTests : IDisposable
{
    private readonly CalisanlarOrtami _ortam = new();

    public void Dispose() => _ortam.Dispose();

    public static TheoryData<string, string, string?, Rol[]> R16Tablosu => new()
    {
        { "genel.mudur@polsan.example", "Genel Müdür", null, [Rol.Calisan, Rol.Yonetici] },
        { "ik1@polsan.example", "İK-1", "genel.mudur@polsan.example", [Rol.Calisan, Rol.Ik] },
        { "ik2@polsan.example", "İK-2", "genel.mudur@polsan.example", [Rol.Calisan, Rol.Ik] },
        { "yonetici.a@polsan.example", "Yönetici A", "genel.mudur@polsan.example", [Rol.Calisan, Rol.Yonetici] },
        { "yonetici.b@polsan.example", "Yönetici B", "genel.mudur@polsan.example", [Rol.Calisan, Rol.Yonetici] },
        { "calisan.a1@polsan.example", "Çalışan A1", "yonetici.a@polsan.example", [Rol.Calisan] },
        { "calisan.a2@polsan.example", "Çalışan A2", "yonetici.a@polsan.example", [Rol.Calisan] },
        { "calisan.b1@polsan.example", "Çalışan B1", "yonetici.b@polsan.example", [Rol.Calisan] },
    };

    [Fact]
    public void DemoVeri_SekizCalisan()
    {
        Assert.Equal(8, _ortam.TumCalisanlar().Count);
    }

    [Theory]
    [MemberData(nameof(R16Tablosu))]
    public void DemoVeri_R16RolVeYonetici(string eposta, string kisi, string? yoneticiEposta, Rol[] roller)
    {
        var calisanlar = _ortam.TumCalisanlar();
        var calisan = Assert.Single(calisanlar, c => c.Eposta == eposta);

        Assert.Equal(roller.Order(), calisan.Roller.Select(r => r.Rol).Order());
        var yonetici = calisanlar.SingleOrDefault(c => c.Id == calisan.YoneticiId);
        Assert.True(yoneticiEposta == yonetici?.Eposta, $"{kisi}: yönetici beklenen {yoneticiEposta}, bulunan {yonetici?.Eposta}");
    }

    [Fact]
    public void DemoVeri_HiyerarsiKurallari()
    {
        var calisanlar = _ortam.TumCalisanlar();
        var kimlikler = calisanlar.ToDictionary(c => c.Id);

        // BR-16: en az iki İK.
        Assert.True(calisanlar.Count(c => c.Roller.Any(r => r.Rol == Rol.Ik)) >= 2);
        // Tam olarak bir tepe (BR-14).
        Assert.Equal(DemoVeri.GenelMudur, Assert.Single(calisanlar, c => c.YoneticiId is null).Id);
        // Her yönetici kaydı var olan bir çalışana bağlı (tek yönetici: YoneticiId tek alandır).
        Assert.All(calisanlar.Where(c => c.YoneticiId is not null), c => Assert.Contains(c.YoneticiId!.Value, kimlikler.Keys));
        // Döngü yok: her zincir tepeye ulaşır.
        Assert.All(calisanlar, c =>
        {
            var ziyaret = new HashSet<Guid>();
            for (var k = c; k.YoneticiId is { } y; k = kimlikler[y])
            {
                Assert.True(ziyaret.Add(k.Id), $"{c.AdSoyad} için hiyerarşide döngü");
            }
        });
        // Astı olan herkes Yönetici rolünde (R-16, G6).
        var yoneticiler = calisanlar.Where(c => c.YoneticiId is not null).Select(c => c.YoneticiId!.Value).Distinct();
        Assert.All(yoneticiler, id => Assert.Contains(kimlikler[id].Roller, r => r.Rol == Rol.Yonetici));
        // Herkes Çalışan rolünde.
        Assert.All(calisanlar, c => Assert.Contains(c.Roller, r => r.Rol == Rol.Calisan));
    }

    [Fact]
    public async Task DemoVeri_HerCalisanDogrulanir()
    {
        foreach (var demo in DemoVeri.Calisanlar)
        {
            var sonuc = await _ortam.DogrulaAsync(demo.Eposta, DemoVeri.Parola);

            Assert.True(sonuc.Basarili, demo.Eposta);
            Assert.Equal(demo.Id, sonuc.Deger.CalisanId);
        }
    }

    [Fact]
    public void SemaKur_IkinciKezDemoVeri_Cogalmaz()
    {
        CalisanlarModulu.SemaKur(_ortam.Servisler, demoVeriYukle: true);

        var calisanlar = _ortam.TumCalisanlar();
        Assert.Equal(8, calisanlar.Count);
        Assert.Equal(DemoVeri.Calisanlar.Sum(d => d.Roller.Length), calisanlar.Sum(c => c.Roller.Count));
    }

    [Fact]
    public void SemaKur_DemoVeriYok_CalisanOlusmaz()
    {
        using var ortam = new CalisanlarOrtami(demoVeriYukle: false);

        Assert.Empty(ortam.TumCalisanlar());
    }
}
