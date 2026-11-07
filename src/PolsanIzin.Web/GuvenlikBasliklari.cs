namespace PolsanIzin.Web;

/// <summary>Her yanıta güvenlik başlıkları (docs/security.md, Input &amp; output).</summary>
public static class GuvenlikBasliklari
{
    /// <summary>
    /// Pipeline'ın en başında çağrılır. Başlıklar <c>OnStarting</c> ile yazılır: hata işleyicisinin
    /// <c>Response.Clear()</c>'ı sonrasında da (hata sayfası) yanıtta kalırlar.
    /// </summary>
    public static IApplicationBuilder UseGuvenlikBasliklari(this IApplicationBuilder app) =>
        app.Use((baglam, sonraki) =>
        {
            var yanit = baglam.Response;
            yanit.OnStarting(() =>
            {
                yanit.Headers.XContentTypeOptions = "nosniff";
                yanit.Headers.XFrameOptions = "DENY";
                yanit.Headers["Referrer-Policy"] = "same-origin";
                return Task.CompletedTask;
            });
            return sonraki(baglam);
        });
}
