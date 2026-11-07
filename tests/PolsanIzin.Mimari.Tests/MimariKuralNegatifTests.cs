namespace PolsanIzin.Mimari.Tests;

/// <summary>
/// AC-1'in ikinci yarısı: kuralı bozan bir bağımlılık eklendiğinde kural fonksiyonu ihlali bulur.
/// Gerçek kodu bozmadan, izinli grafa tek bir yasak kenar eklenmiş modellerle.
/// </summary>
public class MimariKuralNegatifTests
{
    private static Dictionary<string, IReadOnlyCollection<string>> GecerliModel() =>
        MimariKurallar.IzinliGraf.ToDictionary(k => k.Key, k => (IReadOnlyCollection<string>)k.Value);

    private static Dictionary<string, IReadOnlyCollection<string>> Ekle(string proje, string basvuru)
    {
        var model = GecerliModel();
        model[proje] = [.. model[proje], basvuru];
        return model;
    }

    [Fact]
    public void GecerliModel_HicbirKuralIhlalBulmaz()
    {
        var model = GecerliModel();

        Assert.Empty(MimariKurallar.YB2_GrafDisiBasvurular(model));
        Assert.Empty(MimariKurallar.YB3_YaprakModulBasvurulari(model));
        Assert.Empty(MimariKurallar.YB4_AspNetCoreBasvurulari(model));
        Assert.Empty(MimariKurallar.YB4_GecisliAspNetCoreBagimliliklari(model));
        Assert.Empty(MimariKurallar.YB5_WebVeriErisimi(model));
        Assert.Empty(MimariKurallar.YB6_OrtakBasvurulari(model));
    }

    [Theory]
    [InlineData(MimariKurallar.Calisanlar, MimariKurallar.Izinler)]
    [InlineData(MimariKurallar.Izinler, MimariKurallar.Web)]
    [InlineData(MimariKurallar.Bakiyeler, MimariKurallar.Takvim)]
    public void YB2_KuraliBozanReferans_IhlalBulur(string proje, string basvuru)
    {
        var ihlal = Assert.Single(MimariKurallar.YB2_GrafDisiBasvurular(Ekle(proje, basvuru)));
        Assert.Equal($"{proje} -> {basvuru}", ihlal);
    }

    [Theory]
    [InlineData(MimariKurallar.Takvim, MimariKurallar.Calisanlar)]
    [InlineData(MimariKurallar.Bakiyeler, MimariKurallar.Izinler)]
    public void YB3_KuraliBozanReferans_IhlalBulur(string proje, string basvuru)
    {
        Assert.Single(MimariKurallar.YB3_YaprakModulBasvurulari(Ekle(proje, basvuru)));
    }

    [Theory]
    [InlineData(MimariKurallar.Izinler, "Microsoft.AspNetCore.Http.Abstractions")]
    [InlineData(MimariKurallar.Ortak, "Microsoft.AspNetCore.App")]
    public void YB4_KuraliBozanReferans_IhlalBulur(string proje, string basvuru)
    {
        Assert.Single(MimariKurallar.YB4_AspNetCoreBasvurulari(Ekle(proje, basvuru)));
    }

    [Theory]
    [InlineData(MimariKurallar.Calisanlar, "Microsoft.AspNetCore.Http.Abstractions")]
    [InlineData(MimariKurallar.Izinler, "Microsoft.AspNetCore.Cryptography")]
    public void YB4Gecisli_KuraliBozanPaket_IhlalBulur(string proje, string paket)
    {
        Assert.Single(MimariKurallar.YB4_GecisliAspNetCoreBagimliliklari(Ekle(proje, paket)));
    }

    [Theory]
    [InlineData("Microsoft.AspNetCore.Cryptography.Internal")]
    [InlineData("Microsoft.AspNetCore.Cryptography.KeyDerivation")]
    public void YB4Gecisli_KriptografiPaketi_Izinli(string paket)
    {
        Assert.Empty(MimariKurallar.YB4_GecisliAspNetCoreBagimliliklari(Ekle(MimariKurallar.Calisanlar, paket)));
    }

    [Theory]
    [InlineData("Microsoft.EntityFrameworkCore")]
    [InlineData("Microsoft.EntityFrameworkCore.Sqlite")]
    [InlineData("Microsoft.Data.Sqlite")]
    public void YB5_KuraliBozanReferans_IhlalBulur(string basvuru)
    {
        Assert.Single(MimariKurallar.YB5_WebVeriErisimi(Ekle(MimariKurallar.Web, basvuru)));
    }

    [Theory]
    [InlineData(MimariKurallar.Calisanlar)]
    [InlineData("Microsoft.EntityFrameworkCore")]
    [InlineData("Microsoft.AspNetCore.Mvc")]
    public void YB6_KuraliBozanReferans_IhlalBulur(string basvuru)
    {
        Assert.Single(MimariKurallar.YB6_OrtakBasvurulari(Ekle(MimariKurallar.Ortak, basvuru)));
    }
}
