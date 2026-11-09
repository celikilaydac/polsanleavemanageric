using Microsoft.AspNetCore.Mvc.RazorPages;
using PolsanIzin.Izinler;

namespace PolsanIzin.Web.Pages.Izinler;

/// <summary>Taleplerim sekmesi (spec 0004 R-2): yalnızca oturumdaki çalışanın talepleri, salt-okunur.</summary>
public sealed class TaleplerimModel(IIzinTalebiServisi talepServisi) : PageModel
{
    public IReadOnlyList<TalepSatiri> Talepler { get; private set; } = [];

    // Kimlik yalnızca oturumdan alınır; adres parametresi bağlanmaz (spec 0004 R-1, AC-6).
    public async Task OnGetAsync(CancellationToken iptal) =>
        Talepler = await talepServisi.TaleplerAsync(User.OturumdakiCalisanId(), iptal);
}
