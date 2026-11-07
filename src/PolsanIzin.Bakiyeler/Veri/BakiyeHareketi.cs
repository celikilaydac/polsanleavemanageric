namespace PolsanIzin.Bakiyeler.Veri;

/// <summary>Bir talebin bakiyeye etkisi: <c>Beklemede</c> talebin rezervesi veya onaylanan talebin kesin kullanımı.</summary>
internal sealed class BakiyeHareketi
{
    public Guid Id { get; set; }

    public Guid TalepId { get; set; }

    public Guid CalisanId { get; set; }

    public int Yil { get; set; }

    public int Gun { get; set; }

    public HareketTuru Tur { get; set; }
}

internal enum HareketTuru
{
    Rezerve,
    Kesin,
}
