using System.Globalization;
using System.Text.RegularExpressions;

namespace PolsanIzin.Web.Tests;

/// <summary>
/// Palet kuralları (spec 0005 CB-1, CB-2) stil dosyasının kendisinden okunur: renk değerleri
/// değişirse kontrast yeniden hesaplanır. Saf dosya okuma; uygulama başlatılmaz (fast profil).
/// </summary>
public sealed partial class PaletTests
{
    /// <summary>(ön plan, arka plan, en az oran) — metin 4.5:1, odak / kenar 3:1 (WCAG AA).</summary>
    public static TheoryData<string, string, double> KontrastCiftleri => new()
    {
        // Metin
        { "metin", "zemin", 4.5 }, { "metin", "yuzey", 4.5 }, { "metin", "yuzey-2", 4.5 },
        { "metin-soluk", "zemin", 4.5 }, { "metin-soluk", "yuzey", 4.5 }, { "metin-soluk", "yuzey-2", 4.5 },
        // Bağlantı, vurgu, rol rozetleri
        { "mavi", "zemin", 4.5 }, { "mavi", "yuzey", 4.5 }, { "pembe", "yuzey", 4.5 }, { "pembe", "yuzey-2", 4.5 },
        { "mor", "yuzey-2", 4.5 },
        // Durum metinleri ve rozetler (satır üzerinde gezinirken yuzey-2)
        { "hata", "zemin", 4.5 }, { "hata", "yuzey", 4.5 }, { "hata", "yuzey-2", 4.5 },
        { "basari", "yuzey", 4.5 }, { "basari", "yuzey-2", 4.5 },
        { "uyari", "yuzey", 4.5 }, { "uyari", "yuzey-2", 4.5 },
        { "notr", "yuzey", 4.5 }, { "notr", "yuzey-2", 4.5 },
        // Degrade düğme / açık sekme üzerindeki koyu metin (degradenin her durağı)
        { "dugme-metin", "mavi", 4.5 }, { "dugme-metin", "mor", 4.5 }, { "dugme-metin", "pembe", 4.5 },
        // Arayüz öğeleri: form kenarı ve odak göstergesi
        { "kenar", "yuzey", 3 }, { "kenar", "yuzey-2", 3 },
        { "pembe", "zemin", 3 }, { "pembe", "yuzey-2", 3 },
    };

    private static readonly Lazy<string> _css = new(() => File.ReadAllText(Path.Combine(
        DepoKoku(), "src", "PolsanIzin.Web", "wwwroot", "css", "site.css")));

    [Theory]
    [MemberData(nameof(KontrastCiftleri))]
    public void Palet_KontrastWcagAa(string onPlan, string arkaPlan, double enAz)
    {
        var renkler = Renkler();

        var oran = Kontrast(renkler[onPlan], renkler[arkaPlan]);

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

    private static Dictionary<string, string> Renkler()
    {
        var kok = KokBlogu().Match(_css.Value).Groups["govde"].Value;
        return Degisken().Matches(kok).ToDictionary(m => m.Groups["ad"].Value, m => m.Groups["deger"].Value);
    }

    private static double Kontrast(string a, string b)
    {
        var (la, lb) = (Parlaklik(a), Parlaklik(b));
        return (Math.Max(la, lb) + 0.05) / (Math.Min(la, lb) + 0.05);
    }

    /// <summary>WCAG 2.x göreli parlaklık.</summary>
    private static double Parlaklik(string hex)
    {
        double Kanal(int i)
        {
            var c = int.Parse(hex.AsSpan(1 + (i * 2), 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture) / 255.0;
            return c <= 0.03928 ? c / 12.92 : Math.Pow((c + 0.055) / 1.055, 2.4);
        }

        return (0.2126 * Kanal(0)) + (0.7152 * Kanal(1)) + (0.0722 * Kanal(2));
    }

    private static string DepoKoku()
    {
        var dizin = new DirectoryInfo(AppContext.BaseDirectory);
        while (dizin is not null && !File.Exists(Path.Combine(dizin.FullName, "PolsanIzin.slnx")))
        {
            dizin = dizin.Parent;
        }

        return dizin?.FullName ?? throw new InvalidOperationException("PolsanIzin.slnx bulunamadı.");
    }

    [GeneratedRegex(@":root\s*\{(?<govde>[^}]*)\}")]
    private static partial Regex KokBlogu();

    [GeneratedRegex(@"--(?<ad>[a-z0-9-]+):\s*(?<deger>#[0-9a-fA-F]{6})\s*;")]
    private static partial Regex Degisken();

    [GeneratedRegex(@"#[0-9a-fA-F]{3,8}\b|\b(?:rgba?|hsla?)\(")]
    private static partial Regex SabitRenk();
}
