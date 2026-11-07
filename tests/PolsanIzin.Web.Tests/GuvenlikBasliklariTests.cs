namespace PolsanIzin.Web.Tests;

/// <summary>Güvenlik başlıkları ve çerez bayrakları (docs/security.md, docs/testing.md §6).</summary>
[Trait("Kategori", "Entegrasyon")]
public sealed class GuvenlikBasliklariTests
{
    [Fact]
    public async Task Giris_ProductionOrtami_OturumCereziSecureHttpOnlyLax()
    {
        using var factory = new PolsanIzinFactory { Ortam = "Production" };

        var yanit = await factory.Istemci().GirisYapmisAsync();

        var cerez = Assert.Single(
            yanit.Headers.GetValues("Set-Cookie"),
            c => c.StartsWith(TestYardimcilari.OturumCerezi + "=", StringComparison.Ordinal));
        var bayraklar = cerez.Split(';').Select(b => b.Trim().ToLowerInvariant()).ToList();
        Assert.Contains("secure", bayraklar);
        Assert.Contains("httponly", bayraklar);
        Assert.Contains("samesite=lax", bayraklar);
    }
}
