namespace PolsanIzin.Takvim;

/// <summary>Takvim modülünün public arayüzü (docs/architecture.md, iletişim kuralı 1).</summary>
public interface ITakvimServisi
{
    /// <summary>
    /// <paramref name="baslangic"/> ile <paramref name="bitis"/> arasındaki (ikisi dahil) iş günü
    /// sayısı: hafta sonu ve resmi tatiller sayılmaz (BR-1). Bitiş başlangıçtan önceyse 0.
    /// </summary>
    Task<int> IsGunuSayAsync(DateOnly baslangic, DateOnly bitis, CancellationToken iptal = default);
}
