using PolsanIzin.Ortak;

namespace PolsanIzin.Calisanlar;

public interface IKimlikDogrulamaServisi
{
    /// <summary>
    /// E-posta (büyük/küçük harf ve baştaki/sondaki boşluk duyarsız) ve parolayı doğrular. Başarısız
    /// sonuç, hesabın var olup olmadığını belli etmeyen tek bir hata taşır.
    /// </summary>
    Task<Sonuc<OturumBilgisi>> DogrulaAsync(string eposta, string parola, CancellationToken iptal = default);
}
