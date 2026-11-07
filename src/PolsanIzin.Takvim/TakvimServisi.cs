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

        var sayi = 0;
        for (var gun = baslangic; gun <= bitis; gun = gun.AddDays(1))
        {
            if (gun.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday) && !tatiller.Contains(gun))
            {
                sayi++;
            }
        }

        return sayi;
    }
}
