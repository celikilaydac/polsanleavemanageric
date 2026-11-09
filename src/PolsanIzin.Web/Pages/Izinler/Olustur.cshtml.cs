using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PolsanIzin.Bakiyeler;
using PolsanIzin.Izinler;
using PolsanIzin.Ortak;
using Tur = PolsanIzin.Izinler.IzinTuru;

namespace PolsanIzin.Web.Pages.Izinler;

/// <summary>
/// İzin talebi giriş ekranı (spec 0001). Sayfa yalnızca girdi biçimini ayrıştırır; iş kurallarının
/// tamamı <see cref="IIzinTalebiServisi"/>'ndedir (docs/architecture.md, kural 5).
/// </summary>
public sealed class OlusturModel(
    IIzinTalebiServisi talepServisi,
    IBakiyeServisi bakiyeServisi,
    TimeProvider zaman) : PageModel
{
    public const string TarihBicimi = "yyyy-MM-dd";

    public static readonly IReadOnlyList<(Tur Deger, string Ad)> Turler =
    [
        (Tur.Yillik, "Yıllık"),
        (Tur.Mazeret, "Mazeret"),
        (Tur.Ucretsiz, "Ücretsiz"),
    ];

    // Formda kimlik alanı yoktur: talep her zaman oturumdaki çalışan adına açılır (R-1, AC-27).
    [BindProperty]
    public string? IzinTuru { get; set; }

    [BindProperty]
    public string? Baslangic { get; set; }

    [BindProperty]
    public string? Bitis { get; set; }

    [BindProperty]
    public string? Aciklama { get; set; }

    public int BakiyeYili { get; private set; }

    /// <summary>Bugünün yılı için kullanılabilir bakiye; hak tanımsızsa <c>null</c> (R-3).</summary>
    public int? Bakiye { get; private set; }

    public TalepOzeti? Ozet { get; private set; }

    public List<string> GenelHatalar { get; } = [];

    /// <summary>Gönderim reddedildi: form üstünde duyurulur (yalnızca alan hatası olsa da).</summary>
    public bool TalepAlinmadi { get; private set; }

    public static string TurAdi(Tur tur) => Turler.First(t => t.Deger == tur).Ad;

    public static string TarihMetni(DateOnly tarih) => tarih.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture);

    public async Task OnGetAsync(CancellationToken iptal) => await BakiyeYukleAsync(iptal);

    public async Task<IActionResult> OnPostAsync(CancellationToken iptal)
    {
        var bicimHatali = new HashSet<string>(StringComparer.Ordinal);
        var tur = TuruAyristir(bicimHatali);
        var baslangic = TarihiAyristir(nameof(Baslangic), Baslangic, bicimHatali);
        var bitis = TarihiAyristir(nameof(Bitis), Bitis, bicimHatali);

        var sonuc = await talepServisi.TalepOlusturAsync(
            new TalepOlusturKomutu(OturumdakiCalisan(), tur, baslangic, bitis, Aciklama), iptal);

        if (sonuc.Basarili)
        {
            Ozet = sonuc.Deger;
            IzinTuru = Baslangic = Bitis = Aciklama = null;
            ModelState.Clear();
        }
        else
        {
            HatalariGoster(sonuc.Hatalar, bicimHatali);
        }

        await BakiyeYukleAsync(iptal);
        return Page();
    }

    private Guid OturumdakiCalisan() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private async Task BakiyeYukleAsync(CancellationToken iptal)
    {
        BakiyeYili = zaman.Bugun().Year;
        Bakiye = await bakiyeServisi.KullanilabilirBakiyeAsync(OturumdakiCalisan(), BakiyeYili, iptal);
    }

    private Tur? TuruAyristir(HashSet<string> bicimHatali)
    {
        if (string.IsNullOrEmpty(IzinTuru))
        {
            return null;
        }

        // Yalnızca listedeki adlar: sayısal değerler ("1") ve liste dışı adlar reddedilir (AC-15).
        foreach (var (deger, _) in Turler)
        {
            if (string.Equals(deger.ToString(), IzinTuru, StringComparison.Ordinal))
            {
                return deger;
            }
        }

        bicimHatali.Add(nameof(IzinTuru));
        ModelState.AddModelError(nameof(IzinTuru), "Geçersiz izin türü.");
        return null;
    }

    private DateOnly? TarihiAyristir(string alan, string? deger, HashSet<string> bicimHatali)
    {
        if (string.IsNullOrEmpty(deger))
        {
            return null;
        }

        if (DateOnly.TryParseExact(deger, TarihBicimi, CultureInfo.InvariantCulture, DateTimeStyles.None, out var tarih))
        {
            return tarih;
        }

        bicimHatali.Add(alan);
        ModelState.AddModelError(alan, "Geçerli bir tarih girin.");
        return null;
    }

    private void HatalariGoster(IEnumerable<Hata> hatalar, HashSet<string> bicimHatali)
    {
        TalepAlinmadi = true;
        foreach (var hata in hatalar)
        {
            if (hata.Alan is null)
            {
                GenelHatalar.Add(hata.Mesaj);
            }
            else if (!(bicimHatali.Contains(hata.Alan) && hata.Kod == "ZORUNLU"))
            {
                // Biçimi hatalı alan servise boş gider; "zorunlu" mesajı biçim mesajını tekrarlamasın.
                ModelState.AddModelError(hata.Alan, hata.Mesaj);
            }
        }
    }
}
