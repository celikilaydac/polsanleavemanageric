namespace PolsanIzin.Izinler;

/// <summary>Talep durumu (docs/domain.md); <c>Beklemede</c> dışındakiler son durumdur.</summary>
public enum TalepDurumu
{
    Beklemede,
    Onaylandi,
    Reddedildi,
    IptalEdildi,
}
