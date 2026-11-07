using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;

namespace PolsanIzin.Takvim.Tests;

/// <summary>BR-1 iş günü sayımı; demo tatil verisinde 2026-10-29 resmi tatildir (spec 0001 AC-5…AC-9).</summary>
public sealed class IsGunuSayTests : IDisposable
{
    private readonly SqliteConnection _canliTut;
    private readonly ServiceProvider _servisler;

    public IsGunuSayTests()
    {
        var baglanti = $"Data Source=takvim-{Guid.NewGuid()};Mode=Memory;Cache=Shared";
        _canliTut = new SqliteConnection(baglanti);
        _canliTut.Open();
        var services = new ServiceCollection();
        TakvimModulu.Kaydet(services, baglanti);
        _servisler = services.BuildServiceProvider();
        TakvimModulu.SemaKur(_servisler, demoVeriYukle: true);
    }

    public void Dispose()
    {
        _servisler.Dispose();
        _canliTut.Dispose();
    }

    private async Task<int> SayAsync(DateOnly baslangic, DateOnly bitis)
    {
        using var kapsam = _servisler.CreateScope();
        return await kapsam.ServiceProvider.GetRequiredService<ITakvimServisi>()
            .IsGunuSayAsync(baslangic, bitis, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task IsGunuSay_HaftaSonuArada_HaftaSonuSayilmaz()
    {
        Assert.Equal(2, await SayAsync(new(2026, 10, 30), new(2026, 11, 2)));
    }

    [Fact]
    public async Task IsGunuSay_ResmiTatilArada_TatilSayilmaz()
    {
        Assert.Equal(2, await SayAsync(new(2026, 10, 28), new(2026, 10, 30)));
    }

    [Fact]
    public async Task IsGunuSay_TekIsGunu_BirDoner()
    {
        Assert.Equal(1, await SayAsync(new(2026, 11, 2), new(2026, 11, 2)));
    }

    [Theory]
    [InlineData(2026, 10, 31, 2026, 11, 1)]
    [InlineData(2026, 10, 29, 2026, 10, 29)]
    public async Task IsGunuSay_YalnizHaftaSonuVeTatil_SifirDoner(int y1, int a1, int g1, int y2, int a2, int g2)
    {
        Assert.Equal(0, await SayAsync(new(y1, a1, g1), new(y2, a2, g2)));
    }

    [Fact]
    public async Task IsGunuSay_TamHafta_BesDoner()
    {
        Assert.Equal(5, await SayAsync(new(2026, 11, 2), new(2026, 11, 6)));
    }

    [Fact]
    public async Task IsGunuSay_BitisBaslangictanOnce_SifirDoner()
    {
        Assert.Equal(0, await SayAsync(new(2026, 11, 6), new(2026, 11, 2)));
    }
}
