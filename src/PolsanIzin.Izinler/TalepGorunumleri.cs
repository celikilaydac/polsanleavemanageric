namespace PolsanIzin.Izinler;

/// <summary>"Taleplerim" listesinin bir satırı (spec 0004 R-2).</summary>
public sealed record TalepSatiri(
    Guid TalepId,
    IzinTuru IzinTuru,
    DateOnly Baslangic,
    DateOnly Bitis,
    int IsGunu,
    TalepDurumu Durum,
    string? Aciklama);

/// <summary>"Geçmiş" listesinin bir satırı: bir denetim kaydı ve ilgili talep (spec 0004 R-3).</summary>
public sealed record GecmisKaydi(
    DateTimeOffset ZamanUtc,
    Guid TalepId,
    IzinTuru IzinTuru,
    DateOnly Baslangic,
    DateOnly Bitis,
    TalepDurumu? EskiDurum,
    TalepDurumu YeniDurum,
    Guid YapanCalisanId);
