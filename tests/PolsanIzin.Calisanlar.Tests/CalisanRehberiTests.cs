using Microsoft.Extensions.DependencyInjection;
using PolsanIzin.Calisanlar.Veri;

namespace PolsanIzin.Calisanlar.Tests;

public sealed class CalisanRehberiTests : IDisposable
{
    private readonly CalisanlarOrtami _ortam = new();

    public void Dispose() => _ortam.Dispose();

    private async Task<IReadOnlyDictionary<Guid, string>> AdlarAsync(params Guid[] kimlikler)
    {
        using var kapsam = _ortam.Servisler.CreateScope();
        return await kapsam.ServiceProvider.GetRequiredService<ICalisanRehberi>()
            .AdSoyadlarAsync(kimlikler, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task AdSoyadlar_Kimlikler_AdlarDoner()
    {
        var adlar = await AdlarAsync(DemoVeri.Ik1, DemoVeri.YoneticiA);

        Assert.Equal("Elif Kaya", adlar[DemoVeri.Ik1]);
        Assert.Equal("Zeynep Yıldız", adlar[DemoVeri.YoneticiA]);
        Assert.Equal(2, adlar.Count);
    }

    [Fact]
    public async Task AdSoyadlar_BilinmeyenVeTekrarli_BilinmeyenYokSayilir()
    {
        var adlar = await AdlarAsync(DemoVeri.Ik1, DemoVeri.Ik1, Guid.CreateVersion7());

        Assert.Equal("Elif Kaya", Assert.Single(adlar).Value);
    }

    [Fact]
    public async Task AdSoyadlar_BosListe_BosDoner()
    {
        Assert.Empty(await AdlarAsync());
    }
}
