using Microsoft.Extensions.Time.Testing;

namespace PolsanIzin.Ortak.Tests;

public class BugunTests
{
    [Fact]
    public void Bugun_Utc2130_IstanbulErtesiGun()
    {
        var zaman = new FakeTimeProvider(new DateTimeOffset(2026, 10, 21, 21, 30, 0, TimeSpan.Zero));

        Assert.Equal(new DateOnly(2026, 10, 22), zaman.Bugun());
    }

    [Fact]
    public void Bugun_Utc2059_AyniGun()
    {
        var zaman = new FakeTimeProvider(new DateTimeOffset(2026, 10, 21, 20, 59, 0, TimeSpan.Zero));

        Assert.Equal(new DateOnly(2026, 10, 21), zaman.Bugun());
    }

    [Fact]
    public void Bugun_Utc2100_IstanbulGeceYarisiErtesiGun()
    {
        var zaman = new FakeTimeProvider(new DateTimeOffset(2026, 10, 21, 21, 0, 0, TimeSpan.Zero));

        Assert.Equal(new DateOnly(2026, 10, 22), zaman.Bugun());
    }

    [Fact]
    public void Bugun_SunucuSaatDilimindenBagimsiz_YerelSaatDilimiKullanilmaz()
    {
        var zaman = new FakeTimeProvider(new DateTimeOffset(2026, 10, 21, 23, 30, 0, TimeSpan.Zero));
        zaman.SetLocalTimeZone(TimeZoneInfo.Utc);

        Assert.Equal(new DateOnly(2026, 10, 22), zaman.Bugun());
    }
}
