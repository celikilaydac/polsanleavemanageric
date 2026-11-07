namespace PolsanIzin.Izinler;

/// <summary>
/// İzin talebi oluşturma isteği. <paramref name="CalisanId"/> her zaman oturumdaki çalışandır
/// (spec 0001 R-1); alanlar boş gelebilir, kuralları servis uygular.
/// </summary>
public sealed record TalepOlusturKomutu(
    Guid CalisanId,
    IzinTuru? IzinTuru,
    DateOnly? Baslangic,
    DateOnly? Bitis,
    string? Aciklama);

/// <summary>Oluşturulan talebin ekranda gösterilen özeti (spec 0001 "Başarı" durumu).</summary>
public sealed record TalepOzeti(
    Guid TalepId,
    IzinTuru IzinTuru,
    DateOnly Baslangic,
    DateOnly Bitis,
    int IsGunu,
    TalepDurumu Durum);

/// <summary>Hataların bağlandığı alan adları; ekran form alanlarını bu adlarla eşler.</summary>
public static class TalepAlanlari
{
    public const string IzinTuru = nameof(IzinTuru);
    public const string Baslangic = nameof(Baslangic);
    public const string Bitis = nameof(Bitis);
    public const string Aciklama = nameof(Aciklama);
}
