using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PolsanIzin.Calisanlar.Veri;
using PolsanIzin.Ortak;

namespace PolsanIzin.Calisanlar;

internal sealed class KimlikDogrulamaServisi(CalisanlarDbContext db, IPasswordHasher<Calisan> parolaHasher)
    : IKimlikDogrulamaServisi
{
    public static readonly Hata HataliGiris = new("GIRIS", "E-posta veya parola hatalı");

    // Kayıtlı olmayan e-postada da bir hash doğrulanır: yanıt süresi hesabın varlığını ele vermesin.
    private static readonly Lazy<string> _sahteHash = new(() =>
        new PasswordHasher<Calisan>().HashPassword(null!, Guid.NewGuid().ToString()));

    public async Task<Sonuc<OturumBilgisi>> DogrulaAsync(string eposta, string parola, CancellationToken iptal = default)
    {
        if (string.IsNullOrWhiteSpace(eposta) || string.IsNullOrEmpty(parola))
        {
            return Sonuc.Basarisiz<OturumBilgisi>(HataliGiris);
        }

        var normal = EpostaNormalize.Normalize(eposta);
        var calisan = await db.Calisanlar
            .AsNoTracking()
            .Include(c => c.Roller)
            .SingleOrDefaultAsync(c => c.Eposta == normal, iptal);

        if (calisan is null)
        {
            parolaHasher.VerifyHashedPassword(null!, _sahteHash.Value, parola);
            return Sonuc.Basarisiz<OturumBilgisi>(HataliGiris);
        }

        var dogrulama = parolaHasher.VerifyHashedPassword(calisan, calisan.ParolaHash, parola);
        if (dogrulama == PasswordVerificationResult.Failed)
        {
            return Sonuc.Basarisiz<OturumBilgisi>(HataliGiris);
        }

        var roller = calisan.Roller.Select(r => r.Rol).Order().ToList();
        return Sonuc.Basari(new OturumBilgisi(calisan.Id, calisan.AdSoyad, roller));
    }
}
