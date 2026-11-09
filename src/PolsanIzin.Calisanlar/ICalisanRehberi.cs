namespace PolsanIzin.Calisanlar;

/// <summary>Çalışan adlarının salt-okunur sorgusu (ör. denetim izinde "yapan" adı, spec 0004).</summary>
public interface ICalisanRehberi
{
    /// <summary>Verilen kimliklerin ad soyadları; bilinmeyen kimlikler sonuçta yer almaz.</summary>
    Task<IReadOnlyDictionary<Guid, string>> AdSoyadlarAsync(IEnumerable<Guid> kimlikler, CancellationToken iptal = default);
}
