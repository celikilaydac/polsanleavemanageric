using System.Globalization;
using PolsanIzin.Izinler;
using PolsanIzin.Ortak;

namespace PolsanIzin.Web;

/// <summary>İzin ekranlarının Türkçe metinleri ve tarih / zaman biçimleri (spec 0004 R-4); kodda adlar ASCII kalır.</summary>
public static class IzinMetinleri
{
    public static string TurAdi(IzinTuru tur) => tur switch
    {
        IzinTuru.Yillik => "Yıllık",
        IzinTuru.Mazeret => "Mazeret",
        IzinTuru.Ucretsiz => "Ücretsiz",
        _ => tur.ToString(),
    };

    public static string DurumAdi(TalepDurumu durum) => durum switch
    {
        TalepDurumu.Beklemede => "Beklemede",
        TalepDurumu.Onaylandi => "Onaylandı",
        TalepDurumu.Reddedildi => "Reddedildi",
        TalepDurumu.IptalEdildi => "İptal edildi",
        _ => durum.ToString(),
    };

    public static string Tarih(DateOnly tarih) => tarih.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture);

    /// <summary>UTC anı Europe/Istanbul saatiyle, dakikaya kadar.</summary>
    public static string Zaman(DateTimeOffset an) =>
        an.IstanbulZamani().ToString("dd.MM.yyyy HH:mm", CultureInfo.InvariantCulture);
}
