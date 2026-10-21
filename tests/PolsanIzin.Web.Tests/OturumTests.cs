using System.Net;

namespace PolsanIzin.Web.Tests;

[Trait("Kategori", "Entegrasyon")]
public sealed class OturumTests : IDisposable
{
    private readonly PolsanIzinFactory _factory = new();

    public void Dispose() => _factory.Dispose();

    [Theory]
    [InlineData("/")]
    [InlineData("/Index")]
    [InlineData("/Cikis")]
    public async Task Get_KorunanSayfaOturumYok_GiriseYonlendirir(string adres)
    {
        var yanit = await _factory.Istemci().AlAsync(adres);

        Assert.Equal(HttpStatusCode.Redirect, yanit.StatusCode);
        Assert.StartsWith("http://localhost/Giris?ReturnUrl=", yanit.Konum(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Get_GirisSonrasi_IstenenAdreseDoner()
    {
        var istemci = _factory.Istemci();
        var yonlendirme = await istemci.AlAsync("/Index");
        var donusAdresi = Uri.UnescapeDataString(yonlendirme.Konum()!.Split("ReturnUrl=")[1]);
        Assert.Equal("/Index", donusAdresi);

        var yanit = await istemci.GirisYapAsync("ik1@polsan.example", DemoKullanicilar.Parola, donusAdresi);

        Assert.Equal("/Index", yanit.Konum());
    }

    [Theory]
    [InlineData("https://kotu.example")]
    [InlineData("//kotu.example/yol")]
    public async Task Post_DisDonusAdresi_AnaSayfayaGider(string donusAdresi)
    {
        var yanit = await _factory.Istemci().GirisYapAsync("ik1@polsan.example", DemoKullanicilar.Parola, donusAdresi);

        Assert.Equal(HttpStatusCode.Redirect, yanit.StatusCode);
        Assert.Equal("/", yanit.Konum());
    }

    [Fact]
    public async Task Get_GirisOturumAcik_AnaSayfayaYonlendirir()
    {
        var istemci = _factory.Istemci();
        await istemci.GirisYapmisAsync();

        var yanit = await istemci.AlAsync("/Giris");

        Assert.Equal(HttpStatusCode.Redirect, yanit.StatusCode);
        Assert.Equal("/", yanit.Konum());
    }

    [Fact]
    public async Task Post_Cikis_GiriseDonerVeKorumaGeriGelir()
    {
        var istemci = _factory.Istemci();
        await istemci.GirisYapmisAsync();
        var token = await istemci.TokenAlAsync("/");

        var yanit = await istemci.PostAsync(
            "/Cikis",
            new FormUrlEncodedContent(new Dictionary<string, string> { ["__RequestVerificationToken"] = token }),
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Redirect, yanit.StatusCode);
        Assert.Equal("/Giris", yanit.Konum());
        var anaSayfa = await istemci.AlAsync("/");
        Assert.Equal(HttpStatusCode.Redirect, anaSayfa.StatusCode);
        Assert.StartsWith("http://localhost/Giris", anaSayfa.Konum(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Post_GirisVeCikisAntiforgerysiz_Reddedilir()
    {
        var istemci = _factory.Istemci();
        var bilgiler = new Dictionary<string, string> { ["Eposta"] = "ik1@polsan.example", ["Parola"] = DemoKullanicilar.Parola };

        var giris = await istemci.PostAsync("/Giris", new FormUrlEncodedContent(bilgiler), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, giris.StatusCode);
        Assert.False(giris.OturumCereziYazar());

        await istemci.GirisYapmisAsync();
        var cikis = await istemci.PostAsync("/Cikis", new FormUrlEncodedContent([]), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, cikis.StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await istemci.AlAsync("/")).StatusCode);
    }

    [Fact]
    public async Task Get_8SaatHareketsiz_GirisIster()
    {
        var istemci = _factory.Istemci();
        await istemci.GirisYapmisAsync();

        _factory.Zaman.Advance(TimeSpan.FromHours(8) + TimeSpan.FromMinutes(1));
        var yanit = await istemci.AlAsync("/");

        Assert.Equal(HttpStatusCode.Redirect, yanit.StatusCode);
        Assert.StartsWith("http://localhost/Giris", yanit.Konum(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Get_7SaatAraliklarla_OturumSurer()
    {
        var istemci = _factory.Istemci();
        await istemci.GirisYapmisAsync();

        for (var i = 0; i < 3; i++)
        {
            _factory.Zaman.Advance(TimeSpan.FromHours(7));
            Assert.Equal(HttpStatusCode.OK, (await istemci.AlAsync("/")).StatusCode);
        }
    }

    [Fact]
    public async Task Get_3SaatKullan5SaatBekle_OturumSurer()
    {
        // Plan Risk 4: varsayılan kayan süre burada (son istekten 5 saat sonra) oturumu düşürürdü.
        var istemci = _factory.Istemci();
        await istemci.GirisYapmisAsync();

        _factory.Zaman.Advance(TimeSpan.FromHours(3));
        Assert.Equal(HttpStatusCode.OK, (await istemci.AlAsync("/")).StatusCode);
        _factory.Zaman.Advance(TimeSpan.FromHours(5) + TimeSpan.FromMinutes(30));

        Assert.Equal(HttpStatusCode.OK, (await istemci.AlAsync("/")).StatusCode);
    }
}
