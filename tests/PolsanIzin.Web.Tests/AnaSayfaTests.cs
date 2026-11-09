namespace PolsanIzin.Web.Tests;

[Trait("Kategori", "Entegrasyon")]
public sealed class AnaSayfaTests : IDisposable
{
    private readonly PolsanIzinFactory _factory = new();

    public void Dispose() => _factory.Dispose();

    [Theory]
    [InlineData("ik1@polsan.example", "Elif Kaya", new[] { "Çalışan", "İK" })]
    [InlineData("genel.mudur@polsan.example", "Deniz Aksoy", new[] { "Çalışan", "Yönetici" })]
    [InlineData("calisan.a1@polsan.example", "Ayşe Çelik", new[] { "Çalışan" })]
    public async Task Get_Karsilama_AdVeTurkceRoller(string eposta, string adSoyad, string[] roller)
    {
        var istemci = _factory.Istemci();
        await istemci.GirisYapmisAsync(eposta);

        var belge = await (await istemci.AlAsync("/")).HtmlAsync();

        Assert.Equal($"Hoş geldiniz, {adSoyad}", belge.QuerySelector("h1")?.TextContent.Trim());
        Assert.Equal(roller, belge.QuerySelectorAll("ul.roller li").Select(li => li.TextContent.Trim()));
    }

    [Fact]
    public async Task Get_Menu_AnaSayfaVeCikisVarOlmayanBaglantiYok()
    {
        var istemci = _factory.Istemci();
        await istemci.GirisYapmisAsync();

        var belge = await (await istemci.AlAsync("/")).HtmlAsync();
        var menu = belge.QuerySelector("nav");

        Assert.NotNull(menu);
        Assert.Equal(["/", "/Izinler/Olustur", "/Izinler/Taleplerim", "/Izinler/Gecmis"], menu.QuerySelectorAll("a").Select(a => a.GetAttribute("href")));
        var cikis = Assert.Single(menu.QuerySelectorAll("form"));
        Assert.Equal("/Cikis", cikis.GetAttribute("action"));
        Assert.Equal("Çıkış", cikis.QuerySelector("button")?.TextContent.Trim());

        // Menüdeki her bağlantı gerçekten var olan bir sayfaya gider.
        foreach (var href in menu.QuerySelectorAll("a").Select(a => a.GetAttribute("href")!))
        {
            Assert.Equal(System.Net.HttpStatusCode.OK, (await istemci.AlAsync(href)).StatusCode);
        }
    }
}
