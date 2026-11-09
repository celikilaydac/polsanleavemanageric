using Microsoft.EntityFrameworkCore;
using PolsanIzin.Bakiyeler;
using PolsanIzin.Izinler.Veri;
using PolsanIzin.Ortak;
using PolsanIzin.Takvim;

namespace PolsanIzin.Izinler;

internal sealed class IzinTalebiServisi(
    IzinlerDbContext db,
    ITakvimServisi takvim,
    IBakiyeServisi bakiye,
    TimeProvider zaman) : IIzinTalebiServisi
{
    public const int AciklamaSiniri = 500;

    // ADR 0004: tek süreç + tek SQLite dosyası; BR-3 / BR-2 "kontrol et → yaz" yarışı ve iki modüllü
    // yazım (talep + rezerve) bu kilitle seri hâle gelir.
    private static readonly SemaphoreSlim _kilit = new(1, 1);

    public async Task<Sonuc<TalepOzeti>> TalepOlusturAsync(TalepOlusturKomutu komut, CancellationToken iptal = default)
    {
        ArgumentNullException.ThrowIfNull(komut);

        await _kilit.WaitAsync(iptal);
        try
        {
            return await OlusturAsync(komut, iptal);
        }
        finally
        {
            _kilit.Release();
        }
    }

    private async Task<Sonuc<TalepOzeti>> OlusturAsync(TalepOlusturKomutu komut, CancellationToken iptal)
    {
        // Tarayıcı textarea satır sonlarını "\r\n" gönderir; ekrandaki karakter sayısıyla tutarlı
        // olsun diye uzunluk ve saklanan metin "\n" ile hesaplanır (review 0001 K-4).
        komut = komut with { Aciklama = komut.Aciklama?.ReplaceLineEndings("\n") };

        // Plan Risk 4: önce alan kuralları birlikte; temizse çakışma ve bakiye birlikte.
        var (hatalar, isGunu) = await AlanKurallariAsync(komut, iptal);
        if (hatalar.Count > 0)
        {
            return Sonuc.Basarisiz<TalepOzeti>(hatalar);
        }

        var tur = komut.IzinTuru!.Value;
        var baslangic = komut.Baslangic!.Value;
        var bitis = komut.Bitis!.Value;

        hatalar.AddRange(await CakismaVeBakiyeAsync(komut.CalisanId, tur, baslangic, bitis, isGunu, iptal));
        if (hatalar.Count > 0)
        {
            return Sonuc.Basarisiz<TalepOzeti>(hatalar);
        }

        var talepId = Guid.CreateVersion7();
        if (tur == IzinTuru.Yillik)
        {
            var rezerve = await bakiye.RezerveAsync(komut.CalisanId, baslangic.Year, isGunu, talepId, iptal);
            if (!rezerve.Basarili)
            {
                return Sonuc.Basarisiz<TalepOzeti>(rezerve.Hatalar);
            }
        }

        db.IzinTalepleri.Add(new IzinTalebi
        {
            Id = talepId,
            CalisanId = komut.CalisanId,
            IzinTuru = tur,
            Baslangic = baslangic,
            Bitis = bitis,
            IsGunu = isGunu,
            Aciklama = string.IsNullOrWhiteSpace(komut.Aciklama) ? null : komut.Aciklama,
            Durum = TalepDurumu.Beklemede,
        });
        db.DenetimKayitlari.Add(new DenetimKaydi
        {
            Id = Guid.CreateVersion7(),
            TalepId = talepId,
            YapanCalisanId = komut.CalisanId,
            EskiDurum = null,
            YeniDurum = TalepDurumu.Beklemede,
            ZamanUtc = zaman.GetUtcNow(),
        });

        try
        {
            await db.SaveChangesAsync(iptal);
        }
        catch
        {
            // ADR 0004 telafi: talep yazılamadıysa rezerve geri alınır; hata yine de fırlatılır.
            if (tur == IzinTuru.Yillik)
            {
                await bakiye.RezerveGeriAlAsync(talepId, CancellationToken.None);
            }

            throw;
        }

        return Sonuc.Basari(new TalepOzeti(talepId, tur, baslangic, bitis, isGunu, TalepDurumu.Beklemede));
    }

    private async Task<(List<Hata> Hatalar, int IsGunu)> AlanKurallariAsync(TalepOlusturKomutu komut, CancellationToken iptal)
    {
        var hatalar = new List<Hata>();

        if (komut.IzinTuru is null)
        {
            hatalar.Add(new("ZORUNLU", "İzin türü seçilmelidir.", TalepAlanlari.IzinTuru));
        }
        else if (!Enum.IsDefined(komut.IzinTuru.Value))
        {
            hatalar.Add(new("TUR", "Geçersiz izin türü.", TalepAlanlari.IzinTuru));
        }

        if (komut.Baslangic is null)
        {
            hatalar.Add(new("ZORUNLU", "Başlangıç tarihi zorunludur.", TalepAlanlari.Baslangic));
        }

        if (komut.Bitis is null)
        {
            hatalar.Add(new("ZORUNLU", "Bitiş tarihi zorunludur.", TalepAlanlari.Bitis));
        }

        if (komut.Aciklama is { Length: > AciklamaSiniri })
        {
            hatalar.Add(new("ACIKLAMA", $"Açıklama en fazla {AciklamaSiniri} karakter olabilir.", TalepAlanlari.Aciklama));
        }

        if (komut.Baslangic is not { } baslangic || komut.Bitis is not { } bitis)
        {
            return (hatalar, 0);
        }

        // BR-5, BR-12: "bugün" gönderim anındaki Istanbul günüdür.
        if (baslangic < zaman.Bugun())
        {
            hatalar.Add(new("BR-5", "Başlangıç tarihi bugünden önce olamaz.", TalepAlanlari.Baslangic));
        }

        if (bitis < baslangic)
        {
            hatalar.Add(new("BR-4", "Bitiş tarihi başlangıç tarihinden önce olamaz.", TalepAlanlari.Bitis));
            return (hatalar, 0);
        }

        if (baslangic.Year != bitis.Year)
        {
            hatalar.Add(new("BR-13", "Talep iki takvim yılına yayılamaz; yıl sonunu aşan izin için iki ayrı talep açın.", TalepAlanlari.Bitis));
            return (hatalar, 0);
        }

        var isGunu = await takvim.IsGunuSayAsync(baslangic, bitis, iptal);
        if (isGunu == 0)
        {
            hatalar.Add(new("BR-4", "Seçilen aralıkta iş günü yok; talep en az 1 iş günü içermelidir.", TalepAlanlari.Bitis));
        }

        return (hatalar, isGunu);
    }

    private async Task<List<Hata>> CakismaVeBakiyeAsync(
        Guid calisanId, IzinTuru tur, DateOnly baslangic, DateOnly bitis, int isGunu, CancellationToken iptal)
    {
        var hatalar = new List<Hata>();

        // BR-3: takvim günü kesişimi; yalnızca Beklemede ve Onaylandi talepler sayılır.
        var cakisiyor = await db.IzinTalepleri.AnyAsync(
            t => t.CalisanId == calisanId
                 && (t.Durum == TalepDurumu.Beklemede || t.Durum == TalepDurumu.Onaylandi)
                 && t.Baslangic <= bitis
                 && t.Bitis >= baslangic,
            iptal);
        if (cakisiyor)
        {
            hatalar.Add(new("BR-3", "Bu tarihlerle çakışan bekleyen veya onaylanmış bir izin talebiniz var."));
        }

        if (tur == IzinTuru.Yillik)
        {
            var kullanilabilir = await bakiye.KullanilabilirBakiyeAsync(calisanId, baslangic.Year, iptal);
            if (kullanilabilir is null)
            {
                hatalar.Add(new("BR-2", BakiyeMesajlari.HakTanimsiz));
            }
            else if (isGunu > kullanilabilir.Value)
            {
                hatalar.Add(new("BR-2", BakiyeMesajlari.Yetersiz(baslangic.Year, kullanilabilir.Value, isGunu)));
            }
        }

        return hatalar;
    }
}
