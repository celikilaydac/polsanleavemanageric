using System.Threading.RateLimiting;
using Microsoft.Extensions.Options;

namespace PolsanIzin.Web;

/// <summary>Giriş denemesi sınırı (docs/security.md): options ile yapılandırılır, sabit kodlanmaz.</summary>
public sealed class GirisSecenekleri
{
    public const string Bolum = "Giris";

    /// <summary>Bir IP'nin bir pencerede yapabileceği giriş denemesi sayısı.</summary>
    public int DenemeSiniri { get; set; } = 10;

    /// <summary>Sabit pencere süresi (spec 0002 R-6: 1 dakika).</summary>
    public TimeSpan Pencere { get; set; } = TimeSpan.FromMinutes(1);
}

/// <summary>
/// IP başına sabit pencereli giriş denemesi sınırlayıcısı (yerleşik System.Threading.RateLimiting).
/// Giriş sayfası içinde uygulanır ki reddedilen deneme formu ve Türkçe mesajı aynı sayfada görsün.
/// </summary>
public sealed class GirisSinirlayici : IDisposable
{
    /// <summary>IP'si bilinmeyen istekler (ör. TestServer) için açık bölüm anahtarı.</summary>
    public const string BilinmeyenIp = "bilinmeyen";

    private readonly PartitionedRateLimiter<string> _sinirlayici;

    public GirisSinirlayici(IOptions<GirisSecenekleri> secenekler)
    {
        ArgumentNullException.ThrowIfNull(secenekler);
        var s = secenekler.Value;
        _sinirlayici = PartitionedRateLimiter.Create<string, string>(ip =>
            RateLimitPartition.GetFixedWindowLimiter(ip, _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = s.DenemeSiniri,
                Window = s.Pencere,
                QueueLimit = 0,
                AutoReplenishment = true,
            }));
    }

    /// <summary>Denemeye izin varsa <c>true</c>; pencere dolmuşsa <c>false</c>.</summary>
    public bool DenemeyeIzinVer(string? ip)
    {
        using var izin = _sinirlayici.AttemptAcquire(ip ?? BilinmeyenIp);
        return izin.IsAcquired;
    }

    public void Dispose() => _sinirlayici.Dispose();
}
