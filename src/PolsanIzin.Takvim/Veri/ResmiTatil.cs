namespace PolsanIzin.Takvim.Veri;

/// <summary>Tam gün resmi tatil (BR-11: arife yarım günü yoktur).</summary>
internal sealed class ResmiTatil
{
    public DateOnly Tarih { get; set; }

    public required string Ad { get; set; }
}
