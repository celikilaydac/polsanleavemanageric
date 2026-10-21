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
        var istanbulZamani = TimeZoneInfo.ConvertTime(timeProvider.GetUtcNow(), _istanbul);
        return DateOnly.FromDateTime(istanbulZamani.DateTime);
    }
}
