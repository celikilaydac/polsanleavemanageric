using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PolsanIzin.Calisanlar;

namespace PolsanIzin.Web.Pages;

[AllowAnonymous]
public sealed partial class GirisModel(
    IKimlikDogrulamaServisi kimlikDogrulama,
    GirisSinirlayici sinirlayici,
    ILogger<GirisModel> logger) : PageModel
{
    public const string CokFazlaDeneme = "Çok fazla deneme, lütfen biraz sonra tekrar deneyin";

    [BindProperty]
    [Required(ErrorMessage = "E-posta zorunludur.")]
    public string? Eposta { get; set; }

    [BindProperty]
    [Required(ErrorMessage = "Parola zorunludur.")]
    public string? Parola { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? ReturnUrl { get; set; }

    public string? GenelHata { get; private set; }

    public IActionResult OnGet()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToPage("/Index");
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken iptal)
    {
        // Spec 0002 R-6: deneme, doğrulamadan önce sayılır — doğru parola da sınıra takılır.
        if (!sinirlayici.DenemeyeIzinVer(HttpContext.Connection.RemoteIpAddress?.ToString()))
        {
            LogDenemeSiniriAsildi(logger);
            Response.StatusCode = StatusCodes.Status429TooManyRequests;
            GenelHata = CokFazlaDeneme;
            return FormuYenidenGoster();
        }

        if (!ModelState.IsValid)
        {
            return FormuYenidenGoster();
        }

        var sonuc = await kimlikDogrulama.DogrulaAsync(Eposta!, Parola!, iptal);
        if (!sonuc.Basarili)
        {
            LogBasarisizGiris(logger);
            GenelHata = sonuc.Hatalar[0].Mesaj;
            return FormuYenidenGoster();
        }

        var oturum = sonuc.Deger;
        var kimlik = new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, oturum.CalisanId.ToString()),
                new Claim(ClaimTypes.Name, oturum.AdSoyad),
                .. oturum.Roller.Select(r => new Claim(ClaimTypes.Role, r.ToString())),
            ],
            CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(kimlik),
            new AuthenticationProperties { IsPersistent = false });
        LogOturumAcildi(logger, oturum.CalisanId);

        return Url.IsLocalUrl(ReturnUrl) ? LocalRedirect(ReturnUrl) : RedirectToPage("/Index");
    }

    private PageResult FormuYenidenGoster()
    {
        // R-5: girilen e-posta korunur, parola boşaltılır (parola alanı değerini zaten geri yazmaz).
        Parola = null;
        return Page();
    }

    // docs/security.md: loglarda kişisel veri yok — yalnızca ID.
    [LoggerMessage(Level = LogLevel.Information, Message = "Oturum açıldı: {CalisanId}")]
    private static partial void LogOturumAcildi(ILogger logger, Guid calisanId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Başarısız giriş denemesi")]
    private static partial void LogBasarisizGiris(ILogger logger);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Giriş denemesi sınırı aşıldı")]
    private static partial void LogDenemeSiniriAsildi(ILogger logger);
}
