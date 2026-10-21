namespace PolsanIzin.Bakiyeler.Veri;

/// <summary>Bir çalışana bir takvim yılı için tanımlanan toplam yıllık izin günü (docs/domain.md).</summary>
internal sealed class IzinHakki
{
    public Guid CalisanId { get; set; }

    public int Yil { get; set; }

    public int Gun { get; set; }
}
