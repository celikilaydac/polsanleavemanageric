using System.Net;
using Microsoft.Extensions.DependencyInjection;
using PolsanIzin.Calisanlar;
using PolsanIzin.Ortak;

namespace PolsanIzin.Web.Tests;

[Trait("Kategori", "Entegrasyon")]
public sealed class HataSayfasiTests
{
    /// <summary>Üretim koduna hata fırlatan sayfa eklemeden beklenmeyen hata üretir (plan Risk 8).</summary>
    internal sealed class PatlayanKimlikDogrulama : IKimlikDogrulamaServisi
    {
        public Task<Sonuc<OturumBilgisi>> DogrulaAsync(string eposta, string parola, CancellationToken iptal = default) =>
            throw new InvalidOperationException("GIZLI-IC-AYRINTI: veritabanı bağlantısı koptu");
    }

    [Fact]
    public async Task Hata_ProductionOrtami_GenelSayfaStackTraceYok()
    {
        using var factory = new PolsanIzinFactory { Ortam = "Production", TestVerisiYukle = false }
            .ServisEkle(s => s.AddScoped<IKimlikDogrulamaServisi, PatlayanKimlikDogrulama>());
        var istemci = factory.Istemci();

        var yanit = await istemci.GirisYapAsync("ik1@polsan.example", "herhangi");

        Assert.Equal(HttpStatusCode.InternalServerError, yanit.StatusCode);
        var icerik = await yanit.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        Assert.Contains("Bir hata oluştu", icerik, StringComparison.Ordinal);
        Assert.DoesNotContain("GIZLI-IC-AYRINTI", icerik, StringComparison.Ordinal);
        Assert.DoesNotContain("InvalidOperationException", icerik, StringComparison.Ordinal);
        Assert.DoesNotContain("PatlayanKimlikDogrulama", icerik, StringComparison.Ordinal);
        Assert.DoesNotContain("   at ", icerik, StringComparison.Ordinal);
    }
}
