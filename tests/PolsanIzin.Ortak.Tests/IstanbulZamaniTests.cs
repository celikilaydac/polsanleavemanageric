namespace PolsanIzin.Ortak.Tests;

public class IstanbulZamaniTests
{
    [Fact]
    public void IstanbulZamani_Utc0900_Saat1200()
    {
        var zaman = new DateTimeOffset(2026, 10, 21, 9, 0, 0, TimeSpan.Zero).IstanbulZamani();

        Assert.Equal(new DateTime(2026, 10, 21, 12, 0, 0), zaman.DateTime);
        Assert.Equal(TimeSpan.FromHours(3), zaman.Offset);
    }

    [Fact]
    public void IstanbulZamani_Utc2130_ErtesiGun0030()
    {
        var zaman = new DateTimeOffset(2026, 10, 21, 21, 30, 0, TimeSpan.Zero).IstanbulZamani();

        Assert.Equal(new DateTime(2026, 10, 22, 0, 30, 0), zaman.DateTime);
    }

    [Fact]
    public void IstanbulZamani_AyniAn_Degismez()
    {
        var an = new DateTimeOffset(2026, 10, 21, 21, 30, 0, TimeSpan.Zero);

        Assert.Equal(an, an.IstanbulZamani());
    }
}
