namespace PolsanIzin.Takvim.Veri;

/// <summary>
/// Yalnızca Development'ta yüklenen resmi tatiller (spec 0002 R-17): tam günler, arife yok (BR-11).
/// 2027 dini bayram tarihleri Diyanet takvimiyle doğrulandı (Ramazan 9–11 Mart, Kurban 16–19 Mayıs).
/// 19 Mayıs 2027 hem Kurban Bayramı hem Gençlik ve Spor Bayramı'dır; tek gün olarak girilir.
/// </summary>
internal static class DemoVeri
{
    public static readonly IReadOnlyList<ResmiTatil> Tatiller =
    [
        .. Yil(2026, ramazan: new(2026, 3, 20), kurban: new(2026, 5, 27)),
        .. Yil(2027, ramazan: new(2027, 3, 9), kurban: new(2027, 5, 16)),
    ];

    private static IEnumerable<ResmiTatil> Yil(int yil, DateOnly ramazan, DateOnly kurban)
    {
        var gunler = new SortedDictionary<DateOnly, string>
        {
            [new(yil, 1, 1)] = "Yılbaşı",
            [new(yil, 4, 23)] = "Ulusal Egemenlik ve Çocuk Bayramı",
            [new(yil, 5, 1)] = "Emek ve Dayanışma Günü",
            [new(yil, 5, 19)] = "Atatürk'ü Anma, Gençlik ve Spor Bayramı",
            [new(yil, 7, 15)] = "Demokrasi ve Milli Birlik Günü",
            [new(yil, 8, 30)] = "Zafer Bayramı",
            [new(yil, 10, 29)] = "Cumhuriyet Bayramı",
        };

        for (var i = 0; i < 3; i++)
        {
            gunler.TryAdd(ramazan.AddDays(i), $"Ramazan Bayramı {i + 1}. gün");
        }

        for (var i = 0; i < 4; i++)
        {
            gunler.TryAdd(kurban.AddDays(i), $"Kurban Bayramı {i + 1}. gün");
        }

        return gunler.Select(g => new ResmiTatil { Tarih = g.Key, Ad = g.Value });
    }
}
