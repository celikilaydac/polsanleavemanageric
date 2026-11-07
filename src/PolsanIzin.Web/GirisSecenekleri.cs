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
/// IP başına sabit pencereli giriş denemesi sınırlayıcısı. Giriş sayfası içinde uygulanır ki
/// reddedilen deneme formu ve Türkçe mesajı aynı sayfada görsün. Pencere <see cref="TimeProvider"/>
/// ile ölçülür: yerleşik System.Threading.RateLimiting gerçek saate bağlıdır ve testte
/// deterministik değildir (VERIFY V-1, R-03).
/// </summary>
public sealed class GirisSinirlayici(IOptions<GirisSecenekleri> secenekler, TimeProvider zaman)
{
    /// <summary>IP'si bilinmeyen istekler (ör. TestServer) için açık bölüm anahtarı.</summary>
    public const string BilinmeyenIp = "bilinmeyen";

    /// <summary>Bu sayının üstünde, yeni pencere açılırken süresi dolmuş IP kayıtları temizlenir.</summary>
    private const int _temizlikEsigi = 1024;

    private readonly GirisSecenekleri _s = secenekler.Value;
    private readonly Dictionary<string, (DateTimeOffset Baslangic, int Sayi)> _pencereler = [];
    private readonly Lock _kilit = new();

    /// <summary>Denemeye izin varsa <c>true</c>; pencere dolmuşsa <c>false</c>.</summary>
    public bool DenemeyeIzinVer(string? ip)
    {
        var anahtar = ip ?? BilinmeyenIp;
        var simdi = zaman.GetUtcNow();
        lock (_kilit)
        {
            if (_pencereler.TryGetValue(anahtar, out var p) && simdi - p.Baslangic < _s.Pencere)
            {
                if (p.Sayi >= _s.DenemeSiniri)
                {
                    return false;
                }

                _pencereler[anahtar] = (p.Baslangic, p.Sayi + 1);
                return true;
            }

            if (_pencereler.Count > _temizlikEsigi)
            {
                foreach (var eski in _pencereler.Where(k => simdi - k.Value.Baslangic >= _s.Pencere).Select(k => k.Key).ToList())
                {
                    _pencereler.Remove(eski);
                }
            }

            _pencereler[anahtar] = (simdi, 1);
            return true;
        }
    }
}
