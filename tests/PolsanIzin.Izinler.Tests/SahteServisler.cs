using PolsanIzin.Bakiyeler;
using PolsanIzin.Ortak;
using PolsanIzin.Takvim;

namespace PolsanIzin.Izinler.Tests;

/// <summary>
/// İki çağrının aynı noktada gerçekten iç içe geçip geçemediğini ölçer: varan çağrı, diğeri de
/// gelene ya da zaman aşımı dolana kadar bekler (review 0001 K-1). Seri hâle getiren bir kilit
/// varken ikinci çağrı, ilki buradayken gelemez ve <see cref="IcIceGecti"/> <c>false</c> kalır.
/// </summary>
internal sealed class Bulusma(int katilimci, TimeSpan zamanAsimi)
{
    private readonly TaskCompletionSource _tamam = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private int _icerde;

    /// <summary>Aynı anda <c>katilimci</c> kadar çağrı buluşma noktasında bulundu mu?</summary>
    public bool IcIceGecti => _tamam.Task.IsCompleted;

    public async Task BekleAsync()
    {
        if (Interlocked.Increment(ref _icerde) >= katilimci)
        {
            _tamam.TrySetResult();
        }

        try
        {
            await Task.WhenAny(_tamam.Task, Task.Delay(zamanAsimi));
        }
        finally
        {
            Interlocked.Decrement(ref _icerde);
        }
    }
}

/// <summary>Hafta sonu + 2026-10-29 tatil (spec 0001 örneklerindeki takvim).</summary>
internal sealed class SahteTakvim : ITakvimServisi
{
    private static readonly HashSet<DateOnly> _tatiller = [new(2026, 10, 29)];

    /// <summary>Doluysa her çağrı önce bu buluşma noktasında bekler (eşzamanlılık testleri).</summary>
    public Bulusma? Bulusma { get; set; }

    public async Task<int> IsGunuSayAsync(DateOnly baslangic, DateOnly bitis, CancellationToken iptal = default)
    {
        if (Bulusma is not null)
        {
            await Bulusma.BekleAsync();
        }

        var sayi = 0;
        for (var i = 0; i <= bitis.DayNumber - baslangic.DayNumber; i++)
        {
            var gun = baslangic.AddDays(i);
            if (gun.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday) && !_tatiller.Contains(gun))
            {
                sayi++;
            }
        }

        return sayi;
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

    /// <summary>Doluysa geri alma bu hatayla başarısız olur (telafi hatası testi).</summary>
    public Exception? GeriAlHatasi { get; set; }

    public Task<Sonuc> RezerveGeriAlAsync(Guid talepId, CancellationToken iptal = default)
    {
        if (GeriAlHatasi is not null)
        {
            throw GeriAlHatasi;
        }

        GeriAlinanlar.Add(talepId);
        Rezerveler.Remove(talepId);
        return Task.FromResult(Sonuc.Basari());
    }
}
