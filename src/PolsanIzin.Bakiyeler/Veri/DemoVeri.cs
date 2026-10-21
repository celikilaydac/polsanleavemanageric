namespace PolsanIzin.Bakiyeler.Veri;

/// <summary>
/// Yalnızca Development'ta yüklenen izin hakları (spec 0002 R-18): her demo çalışana 2026 için
/// 14 gün; 2027 için bilinçli olarak hak yok. Çalışan kimlikleri Calisanlar'ın demo verisiyle aynı
/// sabit Guid'lerdir (ADR 0003); tutarlılığı Web.Tests'teki çapraz modül testi korur.
/// </summary>
internal static class DemoVeri
{
    public const int Yil = 2026;
    public const int Gun = 14;

    public static readonly IReadOnlyList<Guid> CalisanKimlikleri =
    [
        new("0199f000-0000-7000-8000-000000000001"),
        new("0199f000-0000-7000-8000-000000000002"),
        new("0199f000-0000-7000-8000-000000000003"),
        new("0199f000-0000-7000-8000-000000000004"),
        new("0199f000-0000-7000-8000-000000000005"),
        new("0199f000-0000-7000-8000-000000000006"),
        new("0199f000-0000-7000-8000-000000000007"),
        new("0199f000-0000-7000-8000-000000000008"),
    ];
}
