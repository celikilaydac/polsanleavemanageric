namespace PolsanIzin.Calisanlar.Veri;

internal sealed class Calisan
{
    public Guid Id { get; set; }

    public required string AdSoyad { get; set; }

    /// <summary>Normalize edilmiş e-posta (<see cref="EpostaNormalize.Normalize"/>); tekildir.</summary>
    public required string Eposta { get; set; }

    public required string ParolaHash { get; set; }

    /// <summary>Çalışanın yöneticisi; hiyerarşinin tepesinde <c>null</c> (BR-14).</summary>
    public Guid? YoneticiId { get; set; }

    public List<CalisanRolu> Roller { get; set; } = [];
}

internal sealed class CalisanRolu
{
    public Guid CalisanId { get; set; }

    public Rol Rol { get; set; }
}

internal static class EpostaNormalize
{
    public static string Normalize(string eposta) => eposta.Trim().ToLowerInvariant();
}
