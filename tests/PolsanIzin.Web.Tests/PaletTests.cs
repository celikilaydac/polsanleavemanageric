using System.Globalization;
using System.Text.RegularExpressions;

namespace PolsanIzin.Web.Tests;

/// <summary>
/// Palet kuralları (spec 0005 CB-1, CB-2, PB-3) stil dosyasının kendisinden okunur: renk değerleri
/// değişirse kontrast yeniden hesaplanır. Saf dosya okuma; uygulama başlatılmaz (fast profil).
/// </summary>
public sealed partial class PaletTests
{
    /// <summary>
    /// (ön plan, arka plan, en az oran) — metin 4.5:1, odak / kenar 3:1 (WCAG AA). Arka plan
    /// "ton@zemin" biçimindeyse yarı saydam ton (color-mix) o zeminin üzerine harmanlanır.
    /// </summary>
    public static TheoryData<string, string, double> KontrastCiftleri => new()
    {
        // Metin
        { "metin", "zemin", 4.5 }, { "metin", "yuzey", 4.5 }, { "metin", "yuzey-2", 4.5 },
        { "metin-soluk", "zemin", 4.5 }, { "metin-soluk", "yuzey", 4.5 }, { "metin-soluk", "yuzey-2", 4.5 },
        // Bağlantı, vurgu, rol rozetleri; degrade uygulama adının her durağı zemin üzerinde
        { "mavi", "zemin", 4.5 }, { "mavi", "yuzey", 4.5 }, { "pembe", "yuzey", 4.5 }, { "pembe", "yuzey-2", 4.5 },
        { "mor", "zemin", 4.5 }, { "mor", "yuzey-2", 4.5 }, { "pembe", "zemin", 4.5 },
        // Durum metinleri ve rozetler (satır üzerinde gezinirken yuzey-2)
        { "hata", "zemin", 4.5 }, { "hata", "yuzey", 4.5 }, { "hata", "yuzey-2", 4.5 },
        { "basari", "yuzey", 4.5 }, { "basari", "yuzey-2", 4.5 },
        { "uyari", "yuzey", 4.5 }, { "uyari", "yuzey-2", 4.5 },
        { "notr", "yuzey", 4.5 }, { "notr", "yuzey-2", 4.5 },
        // Mesaj kutuları: yarı saydam ton kartın (yuzey) üzerinde
        { "metin", "hata-zemin@yuzey", 4.5 }, { "hata", "hata-zemin@yuzey", 4.5 },
        { "basari", "basari-zemin@yuzey", 4.5 }, { "metin", "basari-zemin@yuzey", 4.5 },
        { "metin-soluk", "basari-zemin@yuzey", 4.5 },
        // Üst bar: yarı saydam zemin, sayfa zemini üzerinde
        { "metin-soluk", "ust-bar@zemin", 4.5 }, { "metin", "ust-bar@zemin", 4.5 },
        // Degrade düğme / açık sekme üzerindeki koyu metin (degradenin her durağı)
        { "dugme-metin", "mavi", 4.5 }, { "dugme-metin", "mor", 4.5 }, { "dugme-metin", "pembe", 4.5 },
        // Arayüz öğeleri: form kenarı ve odak göstergesi
        { "kenar", "zemin", 3 }, { "kenar", "yuzey", 3 }, { "kenar", "yuzey-2", 3 },
        { "pembe", "zemin", 3 }, { "pembe", "yuzey-2", 3 }, { "pembe", "hata-zemin@yuzey", 3 },
    };

    private static readonly Lazy<string> _css = new(() => File.ReadAllText(WebYolu("wwwroot", "css", "site.css")));

    [Theory]
    [MemberData(nameof(KontrastCiftleri))]
    public void Palet_KontrastWcagAa(string onPlan, string arkaPlan, double enAz)
    {
        var oran = Kontrast(Renk(onPlan), Renk(arkaPlan));

        Assert.True(oran >= enAz, $"--{onPlan} / --{arkaPlan}: {oran:F2}:1 < {enAz}:1");
    }

    [Fact]
    public void Css_DegiskenlerDisindaSabitRenkYok()
    {
        var kok = KokBlogu().Match(_css.Value);
        Assert.True(kok.Success, ":root bloğu yok.");
        var disari = _css.Value.Remove(kok.Index, kok.Length);

        Assert.Empty(SabitRenk().Matches(disari).Select(m => m.Value));
    }

    [Fact]
    public void Palet_TonlarTabanDegiskendenTuretilir()
    {
        // Yarı saydam tonlar sabit rgb kopyası değil, taban rengin color-mix'i olmalı: kesin palet
        // değerleri gelince tonlar da birlikte değişir (review 0005 U-4).
        var kok = KokBlogu().Match(_css.Value).Groups["govde"].Value;
        var tonlar = Ton().Matches(kok).Select(m => m.Groups["ad"].Value).ToHashSet();

        Assert.Superset(new HashSet<string> { "hata-zemin", "basari-zemin", "ust-bar", "isik-mor", "isik-pembe" }, tonlar);
        Assert.All(tonlar, t => Assert.True(Renkler().ContainsKey(Ton().Matches(kok).First(m => m.Groups["ad"].Value == t).Groups["taban"].Value)));
    }

    [Fact]
    public void Sayfalar_SatirIciStilYok()
    {
        var ihlaller = Directory.GetFiles(WebYolu("Pages"), "*.cshtml", SearchOption.AllDirectories)
            .Where(d => SatirIciStil().IsMatch(File.ReadAllText(d)))
            .Select(Path.GetFileName)
            .ToList();

        Assert.Empty(ihlaller);
    }

    [Fact]
    public void Css_DisKaynakYuklemez()
    {
        Assert.DoesNotContain("@import", _css.Value, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("url(", _css.Value, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("@font-face", _css.Value, StringComparison.OrdinalIgnoreCase);
    }

    private static Dictionary<string, string> Renkler()
    {
        var kok = KokBlogu().Match(_css.Value).Groups["govde"].Value;
        return Degisken().Matches(kok).ToDictionary(m => m.Groups["ad"].Value, m => m.Groups["deger"].Value);
    }

    /// <summary>"ad" veya "ton@zemin": ton, color-mix yüzdesiyle zeminin üzerine harmanlanır.</summary>
    private static (double R, double G, double B) Renk(string ad)
    {
        var renkler = Renkler();
        if (!ad.Contains('@', StringComparison.Ordinal))
        {
            return Rgb(renkler[ad]);
        }

        var (tonAdi, zeminAdi) = (ad.Split('@')[0], ad.Split('@')[1]);
        var kok = KokBlogu().Match(_css.Value).Groups["govde"].Value;
        var ton = Ton().Matches(kok).Single(m => m.Groups["ad"].Value == tonAdi);
        var oran = double.Parse(ton.Groups["yuzde"].Value, CultureInfo.InvariantCulture) / 100;
        var (taban, zemin) = (Rgb(renkler[ton.Groups["taban"].Value]), Rgb(renkler[zeminAdi]));
        return (
            (taban.R * oran) + (zemin.R * (1 - oran)),
            (taban.G * oran) + (zemin.G * (1 - oran)),
            (taban.B * oran) + (zemin.B * (1 - oran)));
    }

    private static (double R, double G, double B) Rgb(string hex)
    {
        double Kanal(int i) => int.Parse(hex.AsSpan(1 + (i * 2), 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture) / 255.0;
        return (Kanal(0), Kanal(1), Kanal(2));
    }

    private static double Kontrast((double R, double G, double B) a, (double R, double G, double B) b)
    {
        var (la, lb) = (Parlaklik(a), Parlaklik(b));
        return (Math.Max(la, lb) + 0.05) / (Math.Min(la, lb) + 0.05);
    }

    /// <summary>WCAG 2.x göreli parlaklık.</summary>
    private static double Parlaklik((double R, double G, double B) c)
    {
        static double Dogrusal(double k) => k <= 0.03928 ? k / 12.92 : Math.Pow((k + 0.055) / 1.055, 2.4);
        return (0.2126 * Dogrusal(c.R)) + (0.7152 * Dogrusal(c.G)) + (0.0722 * Dogrusal(c.B));
    }

    private static string WebYolu(params string[] parcalar)
    {
        var dizin = new DirectoryInfo(AppContext.BaseDirectory);
        while (dizin is not null && !File.Exists(Path.Combine(dizin.FullName, "PolsanIzin.slnx")))
        {
            dizin = dizin.Parent;
        }

        var kok = dizin?.FullName ?? throw new InvalidOperationException("PolsanIzin.slnx bulunamadı.");
        return Path.Combine([kok, "src", "PolsanIzin.Web", .. parcalar]);
    }

    [GeneratedRegex(@":root\s*\{(?<govde>[^}]*)\}")]
    private static partial Regex KokBlogu();

    [GeneratedRegex(@"--(?<ad>[a-z0-9-]+):\s*(?<deger>#[0-9a-fA-F]{6})\s*;")]
    private static partial Regex Degisken();

    [GeneratedRegex(@"--(?<ad>[a-z0-9-]+):\s*color-mix\(in srgb,\s*var\(--(?<taban>[a-z0-9-]+)\)\s*(?<yuzde>\d+)%,\s*transparent\)\s*;")]
    private static partial Regex Ton();

    /// <summary>Hex, renk fonksiyonları ve adlandırılmış renkler; transparent / currentcolor / sistem renkleri serbest.</summary>
    [GeneratedRegex(@"#[0-9a-fA-F]{3,8}\b|\b(?:rgba?|hsla?|hwb|lab|lch|oklab|oklch|color|color-mix)\(|(?<![\w-])(?:white|black|red|green|blue|yellow|orange|purple|pink|gray|grey|silver|navy|teal|maroon|olive|lime|aqua|fuchsia|gold|indigo|violet|magenta|cyan)(?![\w-])", RegexOptions.IgnoreCase)]
    private static partial Regex SabitRenk();

    [GeneratedRegex(@"\sstyle\s*=|<style[\s>]", RegexOptions.IgnoreCase)]
    private static partial Regex SatirIciStil();
}
