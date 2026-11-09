using Microsoft.EntityFrameworkCore;
using PolsanIzin.Calisanlar.Veri;

namespace PolsanIzin.Calisanlar;

internal sealed class CalisanRehberi(CalisanlarDbContext db) : ICalisanRehberi
{
    public async Task<IReadOnlyDictionary<Guid, string>> AdSoyadlarAsync(IEnumerable<Guid> kimlikler, CancellationToken iptal = default)
    {
        ArgumentNullException.ThrowIfNull(kimlikler);
        var aranan = kimlikler.Distinct().ToList();
        if (aranan.Count == 0)
        {
            return new Dictionary<Guid, string>();
        }

        return await db.Calisanlar
            .AsNoTracking()
            .Where(c => aranan.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id, c => c.AdSoyad, iptal);
    }
}
