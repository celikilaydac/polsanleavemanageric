using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using PolsanIzin.Takvim.Veri;

namespace PolsanIzin.Takvim.Tests;

public sealed class DemoVeriTests : IDisposable
{
    private readonly SqliteConnection _canliTut;
    private readonly ServiceProvider _servisler;

    public DemoVeriTests()
    {
        var baglanti = $"Data Source=takvim-{Guid.NewGuid()};Mode=Memory;Cache=Shared";
        _canliTut = new SqliteConnection(baglanti);
        _canliTut.Open();
        var services = new ServiceCollection();
        TakvimModulu.Kaydet(services, baglanti);
        _servisler = services.BuildServiceProvider();
    }

    public void Dispose()
    {
        _servisler.Dispose();
        _canliTut.Dispose();
    }

    /// <summary>Spec 0002 R-17 listesi, uygulamadan bağımsız olarak elle yazılmış hâli.</summary>
    private static readonly DateOnly[] _r17Listesi =
    [
        new(2026, 1, 1), new(2026, 3, 20), new(2026, 3, 21), new(2026, 3, 22), new(2026, 4, 23),
        new(2026, 5, 1), new(2026, 5, 19), new(2026, 5, 27), new(2026, 5, 28), new(2026, 5, 29),
        new(2026, 5, 30), new(2026, 7, 15), new(2026, 8, 30), new(2026, 10, 29),
        new(2027, 1, 1), new(2027, 3, 9), new(2027, 3, 10), new(2027, 3, 11), new(2027, 4, 23),
        new(2027, 5, 1), new(2027, 5, 16), new(2027, 5, 17), new(2027, 5, 18), new(2027, 5, 19),
        new(2027, 7, 15), new(2027, 8, 30), new(2027, 10, 29),
    ];

    private List<DateOnly> Tatiller()
    {
        using var kapsam = _servisler.CreateScope();
        return [.. kapsam.ServiceProvider.GetRequiredService<TakvimDbContext>().ResmiTatiller.Select(t => t.Tarih).OrderBy(t => t)];
    }

    [Fact]
    public void DemoVeri_R17ListesiyleBirebir()
    {
        TakvimModulu.SemaKur(_servisler, demoVeriYukle: true);

        Assert.Equal(_r17Listesi, Tatiller());
    }

    [Theory]
    [InlineData(2026, 3, 19)]
    [InlineData(2026, 5, 26)]
    [InlineData(2027, 3, 8)]
    [InlineData(2027, 5, 15)]
    public void DemoVeri_ArifelerYok(int yil, int ay, int gun)
    {
        TakvimModulu.SemaKur(_servisler, demoVeriYukle: true);

        Assert.DoesNotContain(new DateOnly(yil, ay, gun), Tatiller());
    }

    [Fact]
    public void SemaKur_IkinciKezDemoVeri_Cogalmaz()
    {
        TakvimModulu.SemaKur(_servisler, demoVeriYukle: true);
        TakvimModulu.SemaKur(_servisler, demoVeriYukle: true);

        Assert.Equal(_r17Listesi.Length, Tatiller().Count);
    }

    [Fact]
    public void SemaKur_DemoVeriYok_TatilOlusmaz()
    {
        TakvimModulu.SemaKur(_servisler, demoVeriYukle: false);

        Assert.Empty(Tatiller());
    }
}
