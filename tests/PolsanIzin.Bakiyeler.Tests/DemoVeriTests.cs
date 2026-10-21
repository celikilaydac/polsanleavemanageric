using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using PolsanIzin.Bakiyeler.Veri;

namespace PolsanIzin.Bakiyeler.Tests;

public sealed class DemoVeriTests : IDisposable
{
    private readonly SqliteConnection _canliTut;
    private readonly ServiceProvider _servisler;

    public DemoVeriTests()
    {
        var baglanti = $"Data Source=bakiyeler-{Guid.NewGuid()};Mode=Memory;Cache=Shared";
        _canliTut = new SqliteConnection(baglanti);
        _canliTut.Open();
        var services = new ServiceCollection();
        BakiyelerModulu.Kaydet(services, baglanti);
        _servisler = services.BuildServiceProvider();
    }

    public void Dispose()
    {
        _servisler.Dispose();
        _canliTut.Dispose();
    }

    private List<IzinHakki> Haklar()
    {
        using var kapsam = _servisler.CreateScope();
        return [.. kapsam.ServiceProvider.GetRequiredService<BakiyelerDbContext>().IzinHaklari];
    }

    [Fact]
    public void DemoVeri_2026On4Gun_2027Yok()
    {
        BakiyelerModulu.SemaKur(_servisler, demoVeriYukle: true);

        var haklar = Haklar();
        Assert.Equal(8, haklar.Count);
        Assert.Equal(8, haklar.Select(h => h.CalisanId).Distinct().Count());
        Assert.All(haklar, h =>
        {
            Assert.Equal(2026, h.Yil);
            Assert.Equal(14, h.Gun);
        });
        Assert.DoesNotContain(haklar, h => h.Yil == 2027);
    }

    [Fact]
    public void SemaKur_IkinciKezDemoVeri_Cogalmaz()
    {
        BakiyelerModulu.SemaKur(_servisler, demoVeriYukle: true);
        BakiyelerModulu.SemaKur(_servisler, demoVeriYukle: true);

        Assert.Equal(8, Haklar().Count);
    }

    [Fact]
    public void SemaKur_DemoVeriYok_HakOlusmaz()
    {
        BakiyelerModulu.SemaKur(_servisler, demoVeriYukle: false);

        Assert.Empty(Haklar());
    }
}
