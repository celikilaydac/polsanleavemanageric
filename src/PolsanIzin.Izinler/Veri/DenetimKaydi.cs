namespace PolsanIzin.Izinler.Veri;

/// <summary>Durum değişikliği kaydı; yalnızca eklenir, değiştirilmez (docs/domain.md).</summary>
internal sealed class DenetimKaydi
{
    public Guid Id { get; set; }

    public Guid TalepId { get; set; }

    public Guid YapanCalisanId { get; set; }

    public TalepDurumu? EskiDurum { get; set; }

    public TalepDurumu YeniDurum { get; set; }

    public DateTimeOffset ZamanUtc { get; set; }
}
