using PolsanIzin.Calisanlar;

namespace PolsanIzin.Web;

/// <summary>Rollerin ekrandaki Türkçe adları (spec 0002 R-12); koddaki adlar ASCII kalır.</summary>
public static class RolAdlari
{
    private static readonly Dictionary<string, string> _adlar = new(StringComparer.Ordinal)
    {
        [nameof(Rol.Calisan)] = "Çalışan",
        [nameof(Rol.Yonetici)] = "Yönetici",
        [nameof(Rol.Ik)] = "İK",
    };

    private static readonly string[] _sira = [nameof(Rol.Calisan), nameof(Rol.Yonetici), nameof(Rol.Ik)];

    public static IReadOnlyList<string> EkranAdlari(IEnumerable<string> roller) =>
        [.. roller.Distinct().OrderBy(r => Array.IndexOf(_sira, r)).Select(r => _adlar.GetValueOrDefault(r, r))];
}
