using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace PolsanIzin.Web.Pages;

/// <summary>Genel hata sayfası: stack trace, SQL veya iç tip adı göstermez (docs/security.md).</summary>
[AllowAnonymous]
[IgnoreAntiforgeryToken]
[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
public sealed class HataModel : PageModel
{
    public void OnGet()
    {
    }

    public void OnPost()
    {
    }
}
