using PolsanIzin.Calisanlar.Veri;

namespace PolsanIzin.Calisanlar.Tests;

public sealed class KimlikDogrulamaTests : IDisposable
{
    private readonly CalisanlarOrtami _ortam = new();

    public void Dispose() => _ortam.Dispose();

    [Fact]
    public async Task Dogrula_DogruBilgiler_OturumBilgisiDoner()
    {
        var sonuc = await _ortam.DogrulaAsync("ik1@polsan.example", DemoVeri.Parola);

        Assert.True(sonuc.Basarili);
        Assert.Equal(DemoVeri.Ik1, sonuc.Deger.CalisanId);
        Assert.Equal("Elif Kaya", sonuc.Deger.AdSoyad);
        Assert.Equal([Rol.Calisan, Rol.Ik], sonuc.Deger.Roller);
    }

    [Fact]
    public async Task Dogrula_BuyukHarfVeBosluklu_Basarili()
    {
        var sonuc = await _ortam.DogrulaAsync("  IK1@Polsan.EXAMPLE \t", DemoVeri.Parola);

        Assert.True(sonuc.Basarili);
        Assert.Equal(DemoVeri.Ik1, sonuc.Deger.CalisanId);
    }

    [Fact]
    public async Task Dogrula_YanlisParolaVeKayitsizEposta_AyniHata()
    {
        var yanlisParola = await _ortam.DogrulaAsync("ik1@polsan.example", "yanlis-parola");
        var kayitsizEposta = await _ortam.DogrulaAsync("yok@polsan.example", DemoVeri.Parola);

        Assert.False(yanlisParola.Basarili);
        Assert.False(kayitsizEposta.Basarili);
        Assert.Equal(yanlisParola.Hatalar, kayitsizEposta.Hatalar);
        Assert.Equal("E-posta veya parola hatalı", Assert.Single(yanlisParola.Hatalar).Mesaj);
    }

    [Theory]
    [InlineData("", "parola")]
    [InlineData("   ", "parola")]
    [InlineData("ik1@polsan.example", "")]
    public async Task Dogrula_BosAlan_Basarisiz(string eposta, string parola)
    {
        var sonuc = await _ortam.DogrulaAsync(eposta, parola);

        Assert.False(sonuc.Basarili);
    }

    [Fact]
    public async Task Dogrula_ParolaBuyukKucukHarfDuyarli_Basarisiz()
    {
        var sonuc = await _ortam.DogrulaAsync("ik1@polsan.example", DemoVeri.Parola.ToUpperInvariant());

        Assert.False(sonuc.Basarili);
    }

    [Fact]
    public void Kaydet_Parola_HashlenmisSaklanir()
    {
        var calisanlar = _ortam.TumCalisanlar();

        Assert.All(calisanlar, c =>
        {
            Assert.False(string.IsNullOrEmpty(c.ParolaHash));
            Assert.DoesNotContain(DemoVeri.Parola, c.ParolaHash, StringComparison.Ordinal);
        });
        // Tuzlu hash: aynı parola iki çalışanda farklı saklanır.
        Assert.Equal(calisanlar.Count, calisanlar.Select(c => c.ParolaHash).Distinct().Count());
    }
}
