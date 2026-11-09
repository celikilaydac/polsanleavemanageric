using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using PolsanIzin.Bakiyeler;
using PolsanIzin.Izinler.Veri;
using PolsanIzin.Takvim;

namespace PolsanIzin.Izinler.Tests;

/// <summary>Taleplerim ve Geçmiş sorguları (spec 0004 R-2, R-3).</summary>
public sealed class TalepSorgulariTests : IDisposable
{
    private static readonly Guid _calisan = Guid.CreateVersion7();
    private static readonly Guid _baskasi = Guid.CreateVersion7();
    private static readonly DateTimeOffset _t0 = new(2026, 10, 21, 9, 0, 0, TimeSpan.Zero);

    private readonly SqliteConnection _canliTut;
    private readonly ServiceProvider _servisler;

    public TalepSorgulariTests()
    {
        var baglanti = $"Data Source=izinler-{Guid.NewGuid()};Mode=Memory;Cache=Shared";
        _canliTut = new SqliteConnection(baglanti);
        _canliTut.Open();
        var services = new ServiceCollection();
        IzinlerModulu.Kaydet(services, baglanti);
        services.AddSingleton<ITakvimServisi>(new SahteTakvim());
        services.AddSingleton<IBakiyeServisi>(new SahteBakiye());
        services.AddSingleton<TimeProvider>(new FakeTimeProvider(_t0));
        _servisler = services.BuildServiceProvider();
        IzinlerModulu.SemaKur(_servisler, demoVeriYukle: false);
    }

    public void Dispose()
    {
        _servisler.Dispose();
        _canliTut.Dispose();
    }

    private static DateOnly T(int ay, int gun) => new(2026, ay, gun);

    private async Task<T> ServisAsync<T>(Func<IIzinTalebiServisi, Task<T>> islem)
    {
        using var kapsam = _servisler.CreateScope();
        return await islem(kapsam.ServiceProvider.GetRequiredService<IIzinTalebiServisi>());
    }

    private Guid Talep(Guid calisan, DateOnly baslangic, DateOnly bitis, TalepDurumu durum = TalepDurumu.Beklemede, string? aciklama = null)
    {
        using var kapsam = _servisler.CreateScope();
        var db = kapsam.ServiceProvider.GetRequiredService<IzinlerDbContext>();
        var id = Guid.CreateVersion7();
        db.IzinTalepleri.Add(new IzinTalebi
        {
            Id = id,
            CalisanId = calisan,
            IzinTuru = IzinTuru.Mazeret,
            Baslangic = baslangic,
            Bitis = bitis,
            IsGunu = 1,
            Aciklama = aciklama,
            Durum = durum,
        });
        db.SaveChanges();
        return id;
    }

    private void Denetim(Guid talepId, TimeSpan sonra, TalepDurumu? eski, TalepDurumu yeni, Guid yapan, Guid? id = null)
    {
        using var kapsam = _servisler.CreateScope();
        var db = kapsam.ServiceProvider.GetRequiredService<IzinlerDbContext>();
        db.DenetimKayitlari.Add(new DenetimKaydi
        {
            Id = id ?? Guid.CreateVersion7(),
            TalepId = talepId,
            YapanCalisanId = yapan,
            EskiDurum = eski,
            YeniDurum = yeni,
            ZamanUtc = _t0 + sonra,
        });
        db.SaveChanges();
    }

    private static CancellationToken Iptal => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Talepler_BaslangicaGoreYenidenEskiye()
    {
        Talep(_calisan, T(11, 2), T(11, 6));
        Talep(_calisan, T(12, 1), T(12, 2));
        Talep(_calisan, T(11, 16), T(11, 16));
        Talep(_calisan, T(11, 16), T(11, 18));

        var talepler = await ServisAsync(s => s.TaleplerAsync(_calisan, Iptal));

        Assert.Equal(
            [(T(12, 1), T(12, 2)), (T(11, 16), T(11, 18)), (T(11, 16), T(11, 16)), (T(11, 2), T(11, 6))],
            talepler.Select(t => (t.Baslangic, t.Bitis)));
    }

    [Fact]
    public async Task Talepler_TumDurumlarDoner()
    {
        foreach (var (durum, i) in Enum.GetValues<TalepDurumu>().Select((d, i) => (d, i)))
        {
            Talep(_calisan, T(11, 2 + i), T(11, 2 + i), durum, aciklama: $"a{i}");
        }

        var talepler = await ServisAsync(s => s.TaleplerAsync(_calisan, Iptal));

        Assert.Equal(Enum.GetValues<TalepDurumu>().Reverse(), talepler.Select(t => t.Durum));
        Assert.Equal(["a3", "a2", "a1", "a0"], talepler.Select(t => t.Aciklama));
    }

    [Fact]
    public async Task Talepler_YalnizKendi()
    {
        var kendi = Talep(_calisan, T(11, 2), T(11, 2));
        Talep(_baskasi, T(11, 3), T(11, 3));

        var talepler = await ServisAsync(s => s.TaleplerAsync(_calisan, Iptal));

        Assert.Equal(kendi, Assert.Single(talepler).TalepId);
    }

    [Fact]
    public async Task Talepler_TalepYok_BosDoner()
    {
        Assert.Empty(await ServisAsync(s => s.TaleplerAsync(_calisan, Iptal)));
    }

    [Fact]
    public async Task Gecmis_ZamanaGoreYenidenEskiye()
    {
        var talep = Talep(_calisan, T(11, 2), T(11, 6), TalepDurumu.Onaylandi);
        Denetim(talep, TimeSpan.Zero, null, TalepDurumu.Beklemede, _calisan);
        Denetim(talep, TimeSpan.FromHours(2), TalepDurumu.Beklemede, TalepDurumu.Onaylandi, _baskasi);

        var gecmis = await ServisAsync(s => s.GecmisAsync(_calisan, Iptal));

        Assert.Equal(2, gecmis.Count);
        Assert.Equal(new GecmisKaydi(_t0.AddHours(2), talep, IzinTuru.Mazeret, T(11, 2), T(11, 6), TalepDurumu.Beklemede, TalepDurumu.Onaylandi, _baskasi), gecmis[0]);
        Assert.Equal(new GecmisKaydi(_t0, talep, IzinTuru.Mazeret, T(11, 2), T(11, 6), null, TalepDurumu.Beklemede, _calisan), gecmis[1]);
    }

    [Fact]
    public async Task Gecmis_EklemeSirasindanBagimsiz_ZamanaGore()
    {
        // Yeni kayda küçük, eski kayda büyük kimlik verilir: kimliğe göre azalan sıralama zamanın tam
        // tersini üretir, böylece sıralamanın zamana dayandığı her koşuda kanıtlanır (review 0004 P-1, Q-1).
        var talep = Talep(_calisan, T(11, 2), T(11, 2));
        Denetim(talep, TimeSpan.FromHours(2), TalepDurumu.Beklemede, TalepDurumu.Onaylandi, _baskasi, new Guid("00000000-0000-7000-8000-000000000001"));
        Denetim(talep, TimeSpan.Zero, null, TalepDurumu.Beklemede, _calisan, new Guid("00000000-0000-7000-8000-000000000002"));

        var gecmis = await ServisAsync(s => s.GecmisAsync(_calisan, Iptal));

        Assert.Equal([_t0.AddHours(2), _t0], gecmis.Select(g => g.ZamanUtc));
    }

    [Fact]
    public async Task Gecmis_EsitZaman_KimligeGoreAzalan()
    {
        // Plan Risk 5: eşit zamanda kayıt kimliği azalan. Aynı milisaniyede üretilen Guid v7'lerin
        // sırası rastgele kısma bağlı olduğundan kimlikler testte açıkça verilir (deterministik).
        var talep = Talep(_calisan, T(11, 2), T(11, 2));
        Denetim(talep, TimeSpan.Zero, null, TalepDurumu.Beklemede, _calisan, new Guid("00000000-0000-7000-8000-000000000001"));
        Denetim(talep, TimeSpan.Zero, TalepDurumu.Beklemede, TalepDurumu.IptalEdildi, _calisan, new Guid("00000000-0000-7000-8000-000000000002"));

        var gecmis = await ServisAsync(s => s.GecmisAsync(_calisan, Iptal));

        Assert.Equal([TalepDurumu.IptalEdildi, TalepDurumu.Beklemede], gecmis.Select(g => g.YeniDurum));
    }

    [Fact]
    public async Task Gecmis_YalnizKendiTalepleri()
    {
        var kendi = Talep(_calisan, T(11, 2), T(11, 2));
        var baskasinin = Talep(_baskasi, T(11, 3), T(11, 3));
        Denetim(kendi, TimeSpan.Zero, null, TalepDurumu.Beklemede, _calisan);
        // Çalışanın başkasının talebinde yaptığı işlem de (ör. yönetici onayı) onun geçmişi değildir.
        Denetim(baskasinin, TimeSpan.FromMinutes(5), TalepDurumu.Beklemede, TalepDurumu.Onaylandi, _calisan);

        var gecmis = await ServisAsync(s => s.GecmisAsync(_calisan, Iptal));

        Assert.Equal(kendi, Assert.Single(gecmis).TalepId);
    }

    [Fact]
    public async Task Gecmis_KayitYok_BosDoner()
    {
        Assert.Empty(await ServisAsync(s => s.GecmisAsync(_calisan, Iptal)));
    }

    [Fact]
    public async Task Gecmis_TalepOlusturuldu_OlusturmaKaydiGorunur()
    {
        var sonuc = await ServisAsync(s => s.TalepOlusturAsync(
            new TalepOlusturKomutu(_calisan, IzinTuru.Yillik, T(11, 2), T(11, 6), null), Iptal));

        var kayit = Assert.Single(await ServisAsync(s => s.GecmisAsync(_calisan, Iptal)));

        Assert.Equal(new GecmisKaydi(_t0, sonuc.Deger.TalepId, IzinTuru.Yillik, T(11, 2), T(11, 6), null, TalepDurumu.Beklemede, _calisan), kayit);
    }
}
