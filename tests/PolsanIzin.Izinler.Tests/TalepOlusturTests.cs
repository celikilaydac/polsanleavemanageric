using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using PolsanIzin.Bakiyeler;
using PolsanIzin.Izinler.Veri;
using PolsanIzin.Ortak;
using PolsanIzin.Takvim;

namespace PolsanIzin.Izinler.Tests;

/// <summary>
/// Talep oluşturma kuralları (spec 0001 R-5…R-8). "Bugün" = 2026-10-21 (Çarşamba, Istanbul);
/// 2026-10-29 tatil; 2026 hakkı 14 gün.
/// </summary>
public sealed class TalepOlusturTests : IDisposable
{
    private static readonly Guid _calisan = Guid.CreateVersion7();

    private readonly SqliteConnection _canliTut;
    private readonly ServiceProvider _servisler;
    private readonly SahteBakiye _bakiye = new();
    private readonly SahteTakvim _takvim = new();
    private readonly FakeTimeProvider _zaman = new(new DateTimeOffset(2026, 10, 21, 9, 0, 0, TimeSpan.Zero));

    public TalepOlusturTests()
    {
        var baglanti = $"Data Source=izinler-{Guid.NewGuid()};Mode=Memory;Cache=Shared";
        _canliTut = new SqliteConnection(baglanti);
        _canliTut.Open();
        var services = new ServiceCollection();
        IzinlerModulu.Kaydet(services, baglanti);
        services.AddSingleton<ITakvimServisi>(_takvim);
        services.AddSingleton<IBakiyeServisi>(_bakiye);
        services.AddSingleton<TimeProvider>(_zaman);
        _servisler = services.BuildServiceProvider();
        IzinlerModulu.SemaKur(_servisler, demoVeriYukle: false);
    }

    public void Dispose()
    {
        _servisler.Dispose();
        _canliTut.Dispose();
    }

    private static DateOnly T(int ay, int gun, int yil = 2026) => new(yil, ay, gun);

    private async Task<Sonuc<TalepOzeti>> OlusturAsync(
        IzinTuru? tur, DateOnly? baslangic, DateOnly? bitis, string? aciklama = null)
    {
        using var kapsam = _servisler.CreateScope();
        return await kapsam.ServiceProvider.GetRequiredService<IIzinTalebiServisi>().TalepOlusturAsync(
            new TalepOlusturKomutu(_calisan, tur, baslangic, bitis, aciklama), TestContext.Current.CancellationToken);
    }

    private T Db<T>(Func<IzinlerDbContext, T> islem)
    {
        using var kapsam = _servisler.CreateScope();
        return islem(kapsam.ServiceProvider.GetRequiredService<IzinlerDbContext>());
    }

    private void MevcutTalep(DateOnly baslangic, DateOnly bitis, TalepDurumu durum) => Db(db =>
    {
        db.IzinTalepleri.Add(new IzinTalebi
        {
            Id = Guid.CreateVersion7(),
            CalisanId = _calisan,
            IzinTuru = IzinTuru.Mazeret,
            Baslangic = baslangic,
            Bitis = bitis,
            IsGunu = 1,
            Durum = durum,
        });
        return db.SaveChanges();
    });

    private (int Talep, int Denetim, int Rezerve) Sayilar() =>
        Db(db => (db.IzinTalepleri.Count(), db.DenetimKayitlari.Count(), _bakiye.Rezerveler.Count));

    private static Hata TekHata(Sonuc sonuc, string kod) => Assert.Single(sonuc.Hatalar, h => h.Kod == kod);

    [Fact]
    public async Task TalepOlustur_GecerliYillik_BeklemedeOzetVeRezerve()
    {
        var sonuc = await OlusturAsync(IzinTuru.Yillik, T(11, 2), T(11, 6));

        Assert.True(sonuc.Basarili);
        Assert.Equal(new TalepOzeti(sonuc.Deger.TalepId, IzinTuru.Yillik, T(11, 2), T(11, 6), 5, TalepDurumu.Beklemede), sonuc.Deger);
        Assert.Equal((2026, 5), _bakiye.Rezerveler[sonuc.Deger.TalepId]);
        Assert.Equal(TalepDurumu.Beklemede, Db(db => db.IzinTalepleri.Single().Durum));
    }

    [Theory]
    [InlineData(IzinTuru.Mazeret)]
    [InlineData(IzinTuru.Ucretsiz)]
    public async Task TalepOlustur_MazeretVeUcretsiz_RezerveCagrilmaz(IzinTuru tur)
    {
        var sonuc = await OlusturAsync(tur, T(11, 2), T(11, 6));

        Assert.True(sonuc.Basarili);
        Assert.Equal(TalepDurumu.Beklemede, sonuc.Deger.Durum);
        Assert.Empty(_bakiye.Rezerveler);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(500)]
    public async Task TalepOlustur_AciklamaBosVe500Karakter_KabulEdilir(object? aciklama)
    {
        var metin = aciklama is int uzunluk ? new string('a', uzunluk) : (string?)aciklama;

        var sonuc = await OlusturAsync(IzinTuru.Mazeret, T(11, 2), T(11, 2), metin);

        Assert.True(sonuc.Basarili);
    }

    [Fact]
    public async Task TalepOlustur_Gecerli_DenetimKaydiYazilir()
    {
        var sonuc = await OlusturAsync(IzinTuru.Mazeret, T(11, 2), T(11, 2));

        var kayit = Db(db => db.DenetimKayitlari.Single());
        Assert.Equal(sonuc.Deger.TalepId, kayit.TalepId);
        Assert.Equal(_calisan, kayit.YapanCalisanId);
        Assert.Null(kayit.EskiDurum);
        Assert.Equal(TalepDurumu.Beklemede, kayit.YeniDurum);
        Assert.Equal(_zaman.GetUtcNow(), kayit.ZamanUtc);
        Assert.Equal(TimeSpan.Zero, kayit.ZamanUtc.Offset);
    }

    [Fact]
    public async Task TalepOlustur_TekGun_KabulEdilir()
    {
        var sonuc = await OlusturAsync(IzinTuru.Yillik, T(11, 2), T(11, 2));

        Assert.Equal(1, sonuc.Deger.IsGunu);
    }

    [Fact]
    public async Task TalepOlustur_HaftaSonuVeTatilArada_IsGunuSayilir()
    {
        Assert.Equal(2, (await OlusturAsync(IzinTuru.Mazeret, T(10, 30), T(11, 2))).Deger.IsGunu);
        Assert.Equal(2, (await OlusturAsync(IzinTuru.Mazeret, T(10, 27), T(10, 28))).Deger.IsGunu);
    }

    [Fact]
    public async Task TalepOlustur_BitisBaslangictanOnce_BR4BitisAlaniHatasi()
    {
        var sonuc = await OlusturAsync(IzinTuru.Mazeret, T(11, 6), T(11, 2));

        var hata = TekHata(sonuc, "BR-4");
        Assert.Equal(TalepAlanlari.Bitis, hata.Alan);
        // "İş günü yok" değil, sıralama hatası söylenmeli (verify 0001 N-1).
        Assert.Equal("Bitiş tarihi başlangıç tarihinden önce olamaz.", hata.Mesaj);
    }

    [Theory]
    [InlineData(10, 31, 11, 1)]
    [InlineData(10, 29, 10, 29)]
    public async Task TalepOlustur_SifirIsGunu_BR4Hatasi(int a1, int g1, int a2, int g2)
    {
        var sonuc = await OlusturAsync(IzinTuru.Mazeret, T(a1, g1), T(a2, g2));

        TekHata(sonuc, "BR-4");
    }

    [Fact]
    public async Task TalepOlustur_BaslangicBugun_KabulEdilir()
    {
        Assert.True((await OlusturAsync(IzinTuru.Mazeret, T(10, 21), T(10, 21))).Basarili);
    }

    [Fact]
    public async Task TalepOlustur_BaslangicDun_BR5Hatasi()
    {
        var sonuc = await OlusturAsync(IzinTuru.Mazeret, T(10, 20), T(10, 21));

        Assert.Equal(TalepAlanlari.Baslangic, TekHata(sonuc, "BR-5").Alan);
    }

    [Fact]
    public async Task TalepOlustur_UtcAksamIstanbulErtesiGun_BugunIstanbulaGore()
    {
        _zaman.SetUtcNow(new DateTimeOffset(2026, 10, 21, 21, 30, 0, TimeSpan.Zero));

        var dun = await OlusturAsync(IzinTuru.Mazeret, T(10, 21), T(10, 22));
        var bugun = await OlusturAsync(IzinTuru.Mazeret, T(10, 22), T(10, 22));

        TekHata(dun, "BR-5");
        Assert.True(bugun.Basarili);
    }

    [Fact]
    public async Task TalepOlustur_YilAsanAralik_BR13Hatasi()
    {
        var sonuc = await OlusturAsync(IzinTuru.Yillik, T(12, 31), T(1, 4, 2027));

        Assert.Contains("iki ayrı talep", TekHata(sonuc, "BR-13").Mesaj, StringComparison.Ordinal);
    }

    [Fact]
    public async Task TalepOlustur_ZorunluAlanlarBos_HerAlanIcinHata()
    {
        var sonuc = await OlusturAsync(null, null, null);

        Assert.Equal(
            [TalepAlanlari.IzinTuru, TalepAlanlari.Baslangic, TalepAlanlari.Bitis],
            sonuc.Hatalar.Where(h => h.Kod == "ZORUNLU").Select(h => h.Alan));
    }

    [Fact]
    public async Task TalepOlustur_ListedeOlmayanTur_TurHatasi()
    {
        var sonuc = await OlusturAsync((IzinTuru)42, T(11, 2), T(11, 2));

        Assert.Equal(TalepAlanlari.IzinTuru, TekHata(sonuc, "TUR").Alan);
        Assert.Equal((0, 0, 0), Sayilar());
    }

    [Fact]
    public async Task TalepOlustur_AciklamaCrlf_SatirSonuBirKarakterSayilir()
    {
        // Ekranda 500 karakter (satır sonu dahil); tarayıcı "\r\n" gönderdiği için ham hâli 501.
        var aciklama = new string('a', 250) + "\r\n" + new string('b', 249);

        var sonuc = await OlusturAsync(IzinTuru.Mazeret, T(11, 2), T(11, 2), aciklama);

        Assert.True(sonuc.Basarili);
        Assert.Equal(new string('a', 250) + "\n" + new string('b', 249), Db(db => db.IzinTalepleri.Single().Aciklama));
    }

    [Fact]
    public async Task TalepOlustur_Aciklama501_AlanHatasi()
    {
        var sonuc = await OlusturAsync(IzinTuru.Mazeret, T(11, 2), T(11, 2), new string('a', 501));

        Assert.Equal(TalepAlanlari.Aciklama, TekHata(sonuc, "ACIKLAMA").Alan);
    }

    [Theory]
    [InlineData(TalepDurumu.Beklemede)]
    [InlineData(TalepDurumu.Onaylandi)]
    public async Task TalepOlustur_BeklemedeVeOnaylandiIleCakisan_BR3Hatasi(TalepDurumu durum)
    {
        MevcutTalep(T(11, 2), T(11, 6), durum);

        var sonuc = await OlusturAsync(IzinTuru.Mazeret, T(11, 5), T(11, 10));

        Assert.Null(TekHata(sonuc, "BR-3").Alan);
    }

    [Theory]
    [InlineData(TalepDurumu.Reddedildi)]
    [InlineData(TalepDurumu.IptalEdildi)]
    public async Task TalepOlustur_ReddedildiVeIptalIleCakisan_KabulEdilir(TalepDurumu durum)
    {
        MevcutTalep(T(11, 2), T(11, 6), durum);

        Assert.True((await OlusturAsync(IzinTuru.Mazeret, T(11, 2), T(11, 6))).Basarili);
    }

    [Fact]
    public async Task TalepOlustur_BitisikAralik_KabulEdilir()
    {
        MevcutTalep(T(11, 2), T(11, 6), TalepDurumu.Beklemede);

        Assert.True((await OlusturAsync(IzinTuru.Mazeret, T(11, 9), T(11, 13))).Basarili);
    }

    [Theory]
    [InlineData(11, 6, 11, 10)] // yeni talep mevcudun bitiş gününde başlar
    [InlineData(10, 28, 11, 2)] // yeni talep mevcudun başlangıç gününde biter
    public async Task TalepOlustur_SinirGunundeDegen_BR3Hatasi(int a1, int g1, int a2, int g2)
    {
        // Takvim günü kesişimi uçlar dahildir (verify 0001 N-2).
        MevcutTalep(T(11, 2), T(11, 6), TalepDurumu.Beklemede);

        TekHata(await OlusturAsync(IzinTuru.Mazeret, T(a1, g1), T(a2, g2)), "BR-3");
    }

    [Fact]
    public async Task TalepOlustur_YalnizHaftaSonuKesisen_BR3Hatasi()
    {
        // Spec Constraints: çakışma takvim günüyle ölçülür; yalnızca hafta sonunda kesişmek de çakışmadır.
        MevcutTalep(T(11, 6), T(11, 8), TalepDurumu.Beklemede);

        var sonuc = await OlusturAsync(IzinTuru.Mazeret, T(11, 7), T(11, 9));

        TekHata(sonuc, "BR-3");
    }

    [Fact]
    public async Task TalepOlustur_HakTanimsizYil_YillikRedMazeretKabul()
    {
        var yillik = await OlusturAsync(IzinTuru.Yillik, T(1, 4, 2027), T(1, 4, 2027));
        var mazeret = await OlusturAsync(IzinTuru.Mazeret, T(1, 4, 2027), T(1, 4, 2027));

        Assert.Equal(BakiyeMesajlari.HakTanimsiz, TekHata(yillik, "BR-2").Mesaj);
        Assert.True(mazeret.Basarili);
    }

    [Fact]
    public async Task TalepOlustur_BakiyeYetersiz_BR2Hatasi()
    {
        // 2026-11-02 – 11-20: 15 iş günü > 14.
        var sonuc = await OlusturAsync(IzinTuru.Yillik, T(11, 2), T(11, 20));

        Assert.Equal(BakiyeMesajlari.Yetersiz(2026, 14, 15), TekHata(sonuc, "BR-2").Mesaj);
    }

    [Fact]
    public async Task TalepOlustur_BakiyeKadar_KabulEdilir()
    {
        // 2026-11-02 – 11-19: tam 14 iş günü = bakiye.
        var sonuc = await OlusturAsync(IzinTuru.Yillik, T(11, 2), T(11, 19));

        Assert.True(sonuc.Basarili);
        Assert.Equal(14, sonuc.Deger.IsGunu);
    }

    [Fact]
    public async Task TalepOlustur_MevcutRezerveVarken_KalanaGoreKarar()
    {
        _bakiye.Rezerveler[Guid.CreateVersion7()] = (2026, 5);

        var on = await OlusturAsync(IzinTuru.Yillik, T(11, 2), T(11, 13));
        var dokuz = await OlusturAsync(IzinTuru.Yillik, T(11, 2), T(11, 12));

        Assert.Equal(BakiyeMesajlari.Yetersiz(2026, 9, 10), TekHata(on, "BR-2").Mesaj);
        Assert.True(dokuz.Basarili);
    }

    [Fact]
    public async Task TalepOlustur_CakismaVeBakiye_IkisiBirlikteDoner()
    {
        MevcutTalep(T(11, 2), T(11, 2), TalepDurumu.Beklemede);

        var sonuc = await OlusturAsync(IzinTuru.Yillik, T(11, 2), T(11, 20));

        Assert.Equal(["BR-3", "BR-2"], sonuc.Hatalar.Select(h => h.Kod));
    }

    [Fact]
    public async Task TalepOlustur_GecmisVe501Karakter_TumHatalarBirlikte()
    {
        var sonuc = await OlusturAsync(IzinTuru.Mazeret, T(10, 20), T(10, 21), new string('a', 501));

        Assert.Equal(["ACIKLAMA", "BR-5"], sonuc.Hatalar.Select(h => h.Kod).Order());
    }

    public static TheoryData<string> RetYollari => ["zorunlu", "tur", "aciklama", "br4", "br5", "br13", "sifir", "br3", "br2", "hak"];

    [Theory]
    [MemberData(nameof(RetYollari))]
    public async Task TalepOlustur_Reddedilen_TalepRezerveDenetimDegismez(string yol)
    {
        MevcutTalep(T(12, 1), T(12, 1), TalepDurumu.Beklemede);
        var once = Sayilar();

        var sonuc = yol switch
        {
            "zorunlu" => await OlusturAsync(null, T(11, 2), T(11, 2)),
            "tur" => await OlusturAsync((IzinTuru)42, T(11, 2), T(11, 2)),
            "aciklama" => await OlusturAsync(IzinTuru.Yillik, T(11, 2), T(11, 2), new string('a', 501)),
            "br4" => await OlusturAsync(IzinTuru.Yillik, T(11, 6), T(11, 2)),
            "br5" => await OlusturAsync(IzinTuru.Yillik, T(10, 20), T(10, 20)),
            "br13" => await OlusturAsync(IzinTuru.Yillik, T(12, 31), T(1, 4, 2027)),
            "sifir" => await OlusturAsync(IzinTuru.Yillik, T(10, 31), T(11, 1)),
            "br3" => await OlusturAsync(IzinTuru.Yillik, T(12, 1), T(12, 1)),
            "br2" => await OlusturAsync(IzinTuru.Yillik, T(11, 2), T(11, 20)),
            _ => await OlusturAsync(IzinTuru.Yillik, T(1, 4, 2027), T(1, 4, 2027)),
        };

        Assert.False(sonuc.Basarili);
        Assert.Equal(once, Sayilar());
    }

    [Fact]
    public async Task TalepOlustur_YazimHatasi_RezerveGeriAlinir()
    {
        // Rezerve yazıldıktan sonra talep yazımı başarısız olsun: denetim tablosu düşürülür.
        _bakiye.RezerveSonrasi = () =>
        {
            using var komut = _canliTut.CreateCommand();
            komut.CommandText = "DROP TABLE DenetimKayitlari";
            komut.ExecuteNonQuery();
        };

        await Assert.ThrowsAsync<DbUpdateException>(() => OlusturAsync(IzinTuru.Yillik, T(11, 2), T(11, 6)));

        Assert.Single(_bakiye.GeriAlinanlar);
        Assert.Empty(_bakiye.Rezerveler);
        Assert.Equal(0, Db(db => db.IzinTalepleri.Count()));
    }

    [Fact]
    public async Task TalepOlustur_YazimVeTelafiHatasi_IkiHataBirlikteVeTalepKimligi()
    {
        _bakiye.RezerveSonrasi = () =>
        {
            using var komut = _canliTut.CreateCommand();
            komut.CommandText = "DROP TABLE DenetimKayitlari";
            komut.ExecuteNonQuery();
        };
        var telafiHatasi = new InvalidOperationException("telafi başarısız");
        _bakiye.GeriAlHatasi = telafiHatasi;

        var hata = await Assert.ThrowsAsync<AggregateException>(() => OlusturAsync(IzinTuru.Yillik, T(11, 2), T(11, 6)));

        Assert.Equal(2, hata.InnerExceptions.Count);
        Assert.IsType<DbUpdateException>(hata.InnerExceptions[0]);
        Assert.Same(telafiHatasi, hata.InnerExceptions[1]);
        var talepId = Assert.Single(_bakiye.Rezerveler.Keys);
        Assert.Contains(talepId.ToString(), hata.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task TalepOlustur_EsZamanliAyniAralik_YalnizBiriKabul()
    {
        // AC-19 / ADR 0004: iki gönderim gerçekten iç içe geçmeye zorlanır. Kilit varken ikinci çağrı
        // buluşmaya ulaşamaz; kilit olmasaydı ikisi de çakışma kontrolünü geçip iki talep yazardı.
        var bulusma = new Bulusma(2, TimeSpan.FromMilliseconds(300));
        _takvim.Bulusma = bulusma;

        var sonuclar = await Task.WhenAll(
            OlusturAsync(IzinTuru.Mazeret, T(11, 2), T(11, 6)),
            OlusturAsync(IzinTuru.Mazeret, T(11, 2), T(11, 6)));

        Assert.False(bulusma.IcIceGecti);
        Assert.Single(sonuclar, s => s.Basarili);
        TekHata(Assert.Single(sonuclar, s => !s.Basarili), "BR-3");
        Assert.Equal(1, Db(db => db.IzinTalepleri.Count()));
    }

    [Fact]
    public async Task TalepOlustur_EsZamanliIkiYillik_EnFazlaBiriKabul()
    {
        // Her biri tek başına sığar (10 iş günü ≤ 14), ikisi birlikte sığmaz; çağrılar iç içe geçmeye zorlanır.
        var bulusma = new Bulusma(2, TimeSpan.FromMilliseconds(300));
        _takvim.Bulusma = bulusma;

        var sonuclar = await Task.WhenAll(
            OlusturAsync(IzinTuru.Yillik, T(11, 2), T(11, 13)),
            OlusturAsync(IzinTuru.Yillik, T(11, 16), T(11, 27)));

        Assert.False(bulusma.IcIceGecti);
        Assert.Single(sonuclar, s => s.Basarili);
        Assert.Equal(4, await _bakiye.KullanilabilirBakiyeAsync(_calisan, 2026, TestContext.Current.CancellationToken));
    }
}
