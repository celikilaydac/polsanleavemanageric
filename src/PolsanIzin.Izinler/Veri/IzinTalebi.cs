namespace PolsanIzin.Izinler.Veri;

internal sealed class IzinTalebi
{
    public Guid Id { get; set; }

    public Guid CalisanId { get; set; }

    public IzinTuru IzinTuru { get; set; }

    public DateOnly Baslangic { get; set; }

    public DateOnly Bitis { get; set; }

    public int IsGunu { get; set; }

    public string? Aciklama { get; set; }

    public TalepDurumu Durum { get; set; }
}
