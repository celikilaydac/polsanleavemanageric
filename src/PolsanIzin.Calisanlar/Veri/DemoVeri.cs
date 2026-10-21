namespace PolsanIzin.Calisanlar.Veri;

/// <summary>
/// Yalnızca Development'ta yüklenen demo çalışanlar (spec 0002 R-16, ADR 0003). Kimlikler sabittir;
/// aynı Guid'ler Bakiyeler'in demo izin haklarında tekrarlanır. Adlar kurgusaldır.
/// </summary>
internal static class DemoVeri
{
    /// <summary>Tüm demo kullanıcıların ortak parolası — yalnızca Development seed verisi.</summary>
    public const string Parola = "Polsan.Demo.2026";

    public static readonly Guid GenelMudur = new("0199f000-0000-7000-8000-000000000001");
    public static readonly Guid Ik1 = new("0199f000-0000-7000-8000-000000000002");
    public static readonly Guid Ik2 = new("0199f000-0000-7000-8000-000000000003");
    public static readonly Guid YoneticiA = new("0199f000-0000-7000-8000-000000000004");
    public static readonly Guid YoneticiB = new("0199f000-0000-7000-8000-000000000005");
    public static readonly Guid CalisanA1 = new("0199f000-0000-7000-8000-000000000006");
    public static readonly Guid CalisanA2 = new("0199f000-0000-7000-8000-000000000007");
    public static readonly Guid CalisanB1 = new("0199f000-0000-7000-8000-000000000008");

    public static readonly IReadOnlyList<DemoCalisan> Calisanlar =
    [
        new(GenelMudur, "Deniz Aksoy", "genel.mudur@polsan.example", null, [Rol.Calisan, Rol.Yonetici]),
        new(Ik1, "Elif Kaya", "ik1@polsan.example", GenelMudur, [Rol.Calisan, Rol.Ik]),
        new(Ik2, "Mert Şahin", "ik2@polsan.example", GenelMudur, [Rol.Calisan, Rol.Ik]),
        new(YoneticiA, "Zeynep Yıldız", "yonetici.a@polsan.example", GenelMudur, [Rol.Calisan, Rol.Yonetici]),
        new(YoneticiB, "Can Demir", "yonetici.b@polsan.example", GenelMudur, [Rol.Calisan, Rol.Yonetici]),
        new(CalisanA1, "Ayşe Çelik", "calisan.a1@polsan.example", YoneticiA, [Rol.Calisan]),
        new(CalisanA2, "Burak Öztürk", "calisan.a2@polsan.example", YoneticiA, [Rol.Calisan]),
        new(CalisanB1, "Selin Arslan", "calisan.b1@polsan.example", YoneticiB, [Rol.Calisan]),
    ];
}

internal sealed record DemoCalisan(Guid Id, string AdSoyad, string Eposta, Guid? YoneticiId, Rol[] Roller);
