using PolsanIzin.Ortak;

namespace PolsanIzin.Bakiyeler;

/// <summary>
/// Bakiyeler modülünün public arayüzü (docs/architecture.md, iletişim kuralı 1). "Bakiye negatif
/// olamaz" değişmezi yalnızca bu modülde korunur.
/// </summary>
public interface IBakiyeServisi
{
    /// <summary>Hak − kesinleşen kullanım − rezerve (gün). O yıl için hak tanımlı değilse <c>null</c>.</summary>
    Task<int?> KullanilabilirBakiyeAsync(Guid calisanId, int yil, CancellationToken iptal = default);

    /// <summary>
    /// <paramref name="gun"/> günü <paramref name="talepId"/> için rezerve eder. Hak tanımsızsa veya
    /// kullanılabilir bakiye yetmiyorsa BR-2 hatası döner ve hiçbir hareket yazılmaz.
    /// </summary>
    Task<Sonuc> RezerveAsync(Guid calisanId, int yil, int gun, Guid talepId, CancellationToken iptal = default);

    /// <summary>Oluşturulamayan talebin rezervesini geri alır (telafi, ADR 0004). Rezerve yoksa işlem yapmaz.</summary>
    Task<Sonuc> RezerveGeriAlAsync(Guid talepId, CancellationToken iptal = default);
}

/// <summary>Bakiye kurallarının kullanıcıya gösterilen sabit metinleri.</summary>
public static class BakiyeMesajlari
{
    /// <summary>Spec 0001 R-3 / AC-22: birebir sabit metin.</summary>
    public const string HakTanimsiz = "Bu yıl için izin hakkınız tanımlı değil";

    public static string Yetersiz(int yil, int kullanilabilir, int istenen) =>
        $"Yetersiz bakiye: {yil} yılı için kullanılabilir {kullanilabilir} gün, talep {istenen} iş günü.";
}
