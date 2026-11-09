using System.Security.Claims;

namespace PolsanIzin.Web;

public static class OturumUzantilari
{
    /// <summary>
    /// Oturumdaki çalışanın kimliği. Sayfalar çalışan kimliğini yalnızca buradan alır; formdan veya
    /// adresten bağlanan bir kimlik yoktur (spec 0001 R-1, spec 0004 R-1).
    /// </summary>
    public static Guid OturumdakiCalisanId(this ClaimsPrincipal kullanici)
    {
        ArgumentNullException.ThrowIfNull(kullanici);
        return Guid.Parse(kullanici.FindFirstValue(ClaimTypes.NameIdentifier)!);
    }
}
