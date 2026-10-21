using System.Security.Claims;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace PolsanIzin.Web.Pages;

public sealed class IndexModel : PageModel
{
    public string AdSoyad { get; private set; } = string.Empty;

    public IReadOnlyList<string> Roller { get; private set; } = [];

    public void OnGet()
    {
        AdSoyad = User.Identity?.Name ?? string.Empty;
        Roller = RolAdlari.EkranAdlari(User.FindAll(ClaimTypes.Role).Select(c => c.Value));
    }
}
