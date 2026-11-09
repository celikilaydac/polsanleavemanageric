namespace PolsanIzin.Ortak;

public static class ZamanUzantilari
{
    private static readonly TimeZoneInfo _istanbul = TimeZoneInfo.FindSystemTimeZoneById("Europe/Istanbul");

    /// <summary>
    /// "Bugün": Europe/Istanbul saat dilimindeki takvim günü, sunucunun saat dilimi ne olursa olsun
    /// (BR-12). Tarih kontrollerinin tamamı bu güne göre yapılır.
    /// </summary>
    public static DateOnly Bugun(this TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(timeProvider);
        return DateOnly.FromDateTime(timeProvider.GetUtcNow().IstanbulZamani().DateTime);
    }

    /// <summary>Bir anın Europe/Istanbul saatindeki karşılığı (ekranda gösterilen zaman, spec 0004 R-3).</summary>
    public static DateTimeOffset IstanbulZamani(this DateTimeOffset an) => TimeZoneInfo.ConvertTime(an, _istanbul);
}
