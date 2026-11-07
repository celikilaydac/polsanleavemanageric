using PolsanIzin.Bakiyeler;
using PolsanIzin.Ortak;
using PolsanIzin.Takvim;

namespace PolsanIzin.Izinler.Tests;

/// <summary>Hafta sonu + 2026-10-29 tatil (spec 0001 örneklerindeki takvim).</summary>
internal sealed class SahteTakvim : ITakvimServisi
{
    private static readonly HashSet<DateOnly> _tatiller = [new(2026, 10, 29)];

    public Task<int> IsGunuSayAsync(DateOnly baslangic, DateOnly bitis, CancellationToken iptal = default)
    {
        var sayi = 0;
        for (var gun = baslangic; gun <= bitis; gun = gun.AddDays(1))
        {
            if (gun.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday) && !_tatiller.Contains(gun))
            {
                sayi++;
            }
        }

        return Task.FromResult(sayi);
    }
}

/// <summary>Bellekte bakiye: yıl → hak; rezerveler talep kimliğiyle tutulur.</summary>
internal sealed class SahteBakiye : IBakiyeServisi
{
    public Dictionary<int, int> Haklar { get; } = new() { [2026] = 14 };

    public Dictionary<Guid, (int Yil, int Gun)> Rezerveler { get; } = [];

    public List<Guid> GeriAlinanlar { get; } = [];

    /// <summary>Rezerve yazıldıktan hemen sonra çalışır (telafi testi için).</summary>
    public Action? RezerveSonrasi { get; set; }

    public Task<int?> KullanilabilirBakiyeAsync(Guid calisanId, int yil, CancellationToken iptal = default) =>
        Task.FromResult(Haklar.TryGetValue(yil, out var hak)
            ? hak - Rezerveler.Values.Where(r => r.Yil == yil).Sum(r => r.Gun)
            : (int?)null);

    public async Task<Sonuc> RezerveAsync(Guid calisanId, int yil, int gun, Guid talepId, CancellationToken iptal = default)
    {
        var kullanilabilir = await KullanilabilirBakiyeAsync(calisanId, yil, iptal);
        if (kullanilabilir is null || gun > kullanilabilir)
        {
            return Sonuc.Basarisiz(new Hata("BR-2", "sahte: yetersiz"));
        }

        Rezerveler[talepId] = (yil, gun);
        RezerveSonrasi?.Invoke();
        return Sonuc.Basari();
    }

    public Task<Sonuc> RezerveGeriAlAsync(Guid talepId, CancellationToken iptal = default)
    {
        GeriAlinanlar.Add(talepId);
        Rezerveler.Remove(talepId);
        return Task.FromResult(Sonuc.Basari());
    }
}
