using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;
using PolsanIzin.Bakiyeler;
using PolsanIzin.Calisanlar;
using PolsanIzin.Takvim;

namespace PolsanIzin.Web.Tests;

/// <summary>
/// Uygulamayı süreç içinde başlatan factory (docs/testing.md). Ortam varsayılan olarak <c>Test</c>'tir
/// (Development seed'i yüklenmez, plan Risk 2); veritabanı paylaşılan adlandırılmış in-memory SQLite,
/// keep-alive bağlantısı factory ömrü boyunca açık kalır. Test kullanıcıları, modüllerin public
/// <c>SemaKur(…, demoVeriYukle: true)</c> girişiyle factory tarafından açıkça yüklenir.
/// </summary>
public class PolsanIzinFactory : WebApplicationFactory<Program>
{
    public static readonly DateTimeOffset BaslangicZamani = new(2026, 10, 21, 9, 0, 0, TimeSpan.Zero);

    private readonly SqliteConnection _canliTut;
    private readonly List<Action<IServiceCollection>> _ekServisler = [];

    public PolsanIzinFactory(string? baglantiDizesi = null)
    {
        BaglantiDizesi = baglantiDizesi ?? $"Data Source=polsanizin-{Guid.NewGuid()};Mode=Memory;Cache=Shared";
        _canliTut = new SqliteConnection(BaglantiDizesi);
        _canliTut.Open();
    }

    public string BaglantiDizesi { get; }

    public FakeTimeProvider Zaman { get; } = new(BaslangicZamani);

    public LogYakalayici Loglar { get; } = new();

    public string Ortam { get; init; } = "Test";

    public bool TestVerisiYukle { get; init; } = true;

    /// <summary>Testler kendi aralarında sınıra takılmasın; AC-9 testleri kendi değerini verir.</summary>
    public int DenemeSiniri { get; init; } = 10_000;

    public TimeSpan Pencere { get; init; } = TimeSpan.FromMinutes(1);

    public PolsanIzinFactory ServisEkle(Action<IServiceCollection> ekle)
    {
        _ekServisler.Add(ekle);
        return this;
    }

    public HttpClient Istemci() =>
        CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, HandleCookies = true });

    public long Say(string tablo)
    {
        using var komut = _canliTut.CreateCommand();
        komut.CommandText = $"SELECT COUNT(*) FROM \"{tablo}\"";
        return (long)komut.ExecuteScalar()!;
    }

    public long SorguSayisi(string sql)
    {
        using var komut = _canliTut.CreateCommand();
        komut.CommandText = sql;
        return (long)komut.ExecuteScalar()!;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(Ortam);
        builder.UseSetting("ConnectionStrings:PolsanIzin", BaglantiDizesi);
        builder.UseSetting("Giris:DenemeSiniri", DenemeSiniri.ToString(System.Globalization.CultureInfo.InvariantCulture));
        builder.UseSetting("Giris:Pencere", Pencere.ToString("c", System.Globalization.CultureInfo.InvariantCulture));
        builder.ConfigureLogging(l => l.SetMinimumLevel(LogLevel.Trace).AddProvider(Loglar));
        builder.ConfigureTestServices(services =>
        {
            services.AddSingleton<TimeProvider>(Zaman);
            foreach (var ekle in _ekServisler)
            {
                ekle(services);
            }
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);
        if (TestVerisiYukle)
        {
            CalisanlarModulu.SemaKur(host.Services, demoVeriYukle: true);
            TakvimModulu.SemaKur(host.Services, demoVeriYukle: true);
            BakiyelerModulu.SemaKur(host.Services, demoVeriYukle: true);
        }

        return host;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            _canliTut.Dispose();
        }
    }
}
