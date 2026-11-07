using Microsoft.EntityFrameworkCore;
using PolsanIzin.Bakiyeler.Veri;
using PolsanIzin.Ortak;

namespace PolsanIzin.Bakiyeler;

internal sealed class BakiyeServisi(BakiyelerDbContext db) : IBakiyeServisi
{
    // "Kontrol et → yaz" yarışına karşı: rezerve kararı ve yazımı seri yapılır; değişmezin sahibi
    // bu modüldür, çağıranın kilidine güvenmez (ADR 0004).
    private static readonly SemaphoreSlim _kilit = new(1, 1);

    public async Task<int?> KullanilabilirBakiyeAsync(Guid calisanId, int yil, CancellationToken iptal = default)
    {
        var hak = await db.IzinHaklari
            .AsNoTracking()
            .Where(h => h.CalisanId == calisanId && h.Yil == yil)
            .Select(h => (int?)h.Gun)
            .SingleOrDefaultAsync(iptal);
        if (hak is null)
        {
            return null;
        }

        var kullanilan = await db.BakiyeHareketleri
            .AsNoTracking()
            .Where(h => h.CalisanId == calisanId && h.Yil == yil)
            .SumAsync(h => h.Gun, iptal);
        return hak.Value - kullanilan;
    }

    public async Task<Sonuc> RezerveAsync(Guid calisanId, int yil, int gun, Guid talepId, CancellationToken iptal = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(gun);

        await _kilit.WaitAsync(iptal);
        try
        {
            var kullanilabilir = await KullanilabilirBakiyeAsync(calisanId, yil, iptal);
            if (kullanilabilir is null)
            {
                return Sonuc.Basarisiz(new Hata("BR-2", BakiyeMesajlari.HakTanimsiz));
            }

            if (gun > kullanilabilir.Value)
            {
                return Sonuc.Basarisiz(new Hata("BR-2", BakiyeMesajlari.Yetersiz(yil, kullanilabilir.Value, gun)));
            }

            db.BakiyeHareketleri.Add(new BakiyeHareketi
            {
                Id = Guid.CreateVersion7(),
                TalepId = talepId,
                CalisanId = calisanId,
                Yil = yil,
                Gun = gun,
                Tur = HareketTuru.Rezerve,
            });
            await db.SaveChangesAsync(iptal);
            return Sonuc.Basari();
        }
        finally
        {
            _kilit.Release();
        }
    }

    public async Task<Sonuc> RezerveGeriAlAsync(Guid talepId, CancellationToken iptal = default)
    {
        await _kilit.WaitAsync(iptal);
        try
        {
            await db.BakiyeHareketleri
                .Where(h => h.TalepId == talepId && h.Tur == HareketTuru.Rezerve)
                .ExecuteDeleteAsync(iptal);
            return Sonuc.Basari();
        }
        finally
        {
            _kilit.Release();
        }
    }
}
