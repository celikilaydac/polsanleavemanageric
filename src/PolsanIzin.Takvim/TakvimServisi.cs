using Microsoft.EntityFrameworkCore;
using PolsanIzin.Takvim.Veri;

namespace PolsanIzin.Takvim;

internal sealed class TakvimServisi(TakvimDbContext db) : ITakvimServisi
{
    public async Task<int> IsGunuSayAsync(DateOnly baslangic, DateOnly bitis, CancellationToken iptal = default)
    {
        if (bitis < baslangic)
        {
            return 0;
        }

        var tatiller = (await db.ResmiTatiller
            .AsNoTracking()
            .Where(t => t.Tarih >= baslangic && t.Tarih <= bitis)
            .Select(t => t.Tarih)
            .ToListAsync(iptal)).ToHashSet();

        // Gün sayısıyla döner: tarihle dönmek DateOnly.MaxValue'da AddDays(1) taşmasına yol açar.
        var sayi = 0;
        for (var i = 0; i <= bitis.DayNumber - baslangic.DayNumber; i++)
        {
            var gun = baslangic.AddDays(i);
            if (gun.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday) && !tatiller.Contains(gun))
            {
                sayi++;
            }
        }

        return sayi;
    }
}
