using System.Security.Claims;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PolsanIzin.Calisanlar;
using PolsanIzin.Izinler;

namespace PolsanIzin.Web.Pages.Izinler;

/// <summary>
/// Geçmiş sekmesi (spec 0004 R-3): oturumdaki çalışanın taleplerinin denetim izi, salt-okunur.
/// "Yapan" adı Calisanlar'dan eşlenir (gösterim; plan 0004 Risk 1).
/// </summary>
public sealed class GecmisModel(IIzinTalebiServisi talepServisi, ICalisanRehberi calisanRehberi) : PageModel
{
    public sealed record Satir(string Zaman, string Talep, string Degisiklik, string Yapan);

    public IReadOnlyList<Satir> Satirlar { get; private set; } = [];

    public async Task OnGetAsync(CancellationToken iptal)
    {
        // Kimlik yalnızca oturumdan alınır; adres parametresi bağlanmaz (spec 0004 R-1, AC-12).
        var ben = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var kayitlar = await talepServisi.GecmisAsync(ben, iptal);
        var adlar = await calisanRehberi.AdSoyadlarAsync(
            kayitlar.Select(k => k.YapanCalisanId).Where(id => id != ben), iptal);

        Satirlar = [.. kayitlar.Select(k => new Satir(
            IzinMetinleri.Zaman(k.ZamanUtc),
            $"{IzinMetinleri.TurAdi(k.IzinTuru)} {IzinMetinleri.Tarih(k.Baslangic)} – {IzinMetinleri.Tarih(k.Bitis)}",
            $"{(k.EskiDurum is { } eski ? IzinMetinleri.DurumAdi(eski) : "Oluşturuldu")} → {IzinMetinleri.DurumAdi(k.YeniDurum)}",
            k.YapanCalisanId == ben ? "Siz" : adlar.GetValueOrDefault(k.YapanCalisanId, "—")))];
    }
}
