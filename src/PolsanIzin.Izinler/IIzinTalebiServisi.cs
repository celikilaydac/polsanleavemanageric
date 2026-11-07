using PolsanIzin.Ortak;

namespace PolsanIzin.Izinler;

/// <summary>Izinler modülünün public arayüzü (docs/architecture.md, iletişim kuralı 1).</summary>
public interface IIzinTalebiServisi
{
    /// <summary>
    /// Kurallar (spec 0001 R-5) sağlanıyorsa talebi <c>Beklemede</c> kaydeder, <c>Yillik</c> ise
    /// bakiyeden rezerve eder ve denetim izine yazar. Aksi hâlde ihlal edilen tüm kurallar döner
    /// ve hiçbir kayıt oluşmaz (R-8).
    /// </summary>
    Task<Sonuc<TalepOzeti>> TalepOlusturAsync(TalepOlusturKomutu komut, CancellationToken iptal = default);
}
