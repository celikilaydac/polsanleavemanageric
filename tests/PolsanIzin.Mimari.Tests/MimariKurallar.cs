namespace PolsanIzin.Mimari.Tests;

/// <summary>
/// YB-1…YB-6'nın saf kural fonksiyonları (docs/architecture.md). Girdi bir bağımlılık modelidir:
/// proje adı → başvurduğu assembly / proje adları. Gerçek assembly'lere <see cref="MimariTests"/>,
/// kuralı bozan sahte modellere <see cref="MimariKuralNegatifTests"/> uygular.
/// </summary>
public static class MimariKurallar
{
    public const string Ortak = "PolsanIzin.Ortak";
    public const string Calisanlar = "PolsanIzin.Calisanlar";
    public const string Takvim = "PolsanIzin.Takvim";
    public const string Bakiyeler = "PolsanIzin.Bakiyeler";
    public const string Izinler = "PolsanIzin.Izinler";
    public const string Web = "PolsanIzin.Web";

    public static readonly string[] Moduller = [Calisanlar, Takvim, Bakiyeler, Izinler];

    public static readonly string[] YaprakModuller = [Calisanlar, Takvim, Bakiyeler];

    /// <summary>YB-2: izinli proje grafiği; buradaki kenarlar dışındaki her PolsanIzin başvurusu ihlaldir.</summary>
    public static readonly IReadOnlyDictionary<string, string[]> IzinliGraf = new Dictionary<string, string[]>
    {
        [Ortak] = [],
        [Calisanlar] = [Ortak],
        [Takvim] = [Ortak],
        [Bakiyeler] = [Ortak],
        [Izinler] = [Ortak, Calisanlar, Takvim, Bakiyeler],
        [Web] = [Ortak, Calisanlar, Takvim, Bakiyeler, Izinler],
    };

    /// <summary>YB-1 girdisi: bir modül tipinin kurala ilişkin özellikleri.</summary>
    public sealed record TipBilgisi(string Ad, bool Public, bool DbContext, bool Entity, bool VeriNamespaceinde);

    /// <summary>YB-1: DbContext, entity (bir DbContext'in <c>DbSet</c>'i) ve <c>.Veri</c> tipleri public olamaz.</summary>
    public static IReadOnlyList<string> YB1_PublicVeriTipleri(IEnumerable<TipBilgisi> tipler) =>
        [.. tipler.Where(t => t.Public && (t.DbContext || t.Entity || t.VeriNamespaceinde)).Select(t => t.Ad)];

    /// <summary>YB-1: bir assembly <c>InternalsVisibleTo</c> ile yalnızca kendi test projesine açılır (ADR 0003).</summary>
    public static IReadOnlyList<string> YB1_InternalsVisibleToIhlalleri(IEnumerable<(string Assembly, string Hedef)> acilimlar) =>
        [.. acilimlar.Where(a => a.Hedef != a.Assembly + ".Tests").Select(a => $"{a.Assembly} -> {a.Hedef}")];

    public static IReadOnlyList<string> YB2_GrafDisiBasvurular(IReadOnlyDictionary<string, IReadOnlyCollection<string>> model)
    {
        ArgumentNullException.ThrowIfNull(model);
        var ihlaller = new List<string>();
        foreach (var (proje, basvurular) in model)
        {
            var izinli = IzinliGraf.TryGetValue(proje, out var k) ? k : [];
            ihlaller.AddRange(basvurular
                .Where(PolsanIzinProjesi)
                .Where(b => !izinli.Contains(b))
                .Select(b => $"{proje} -> {b}"));
        }

        return ihlaller;
    }

    public static IReadOnlyList<string> YB3_YaprakModulBasvurulari(IReadOnlyDictionary<string, IReadOnlyCollection<string>> model) =>
        Ihlaller(model, YaprakModuller, b => PolsanIzinProjesi(b) && b != Ortak);

    public static IReadOnlyList<string> YB4_AspNetCoreBasvurulari(IReadOnlyDictionary<string, IReadOnlyCollection<string>> model) =>
        Ihlaller(model, [Ortak, .. Moduller], AspNetCore);

    /// <summary>
    /// YB-4'ün geçişli yarısı: girdi, proje → geçişli paket kapanışı. Tek istisna parola hash'inin
    /// getirdiği <c>Microsoft.AspNetCore.Cryptography.*</c> (ADR 0003).
    /// </summary>
    public static IReadOnlyList<string> YB4_GecisliAspNetCoreBagimliliklari(IReadOnlyDictionary<string, IReadOnlyCollection<string>> model) =>
        Ihlaller(model, [Ortak, .. Moduller], b =>
            AspNetCore(b) && !b.StartsWith("Microsoft.AspNetCore.Cryptography.", StringComparison.Ordinal));

    public static IReadOnlyList<string> YB5_WebVeriErisimi(IReadOnlyDictionary<string, IReadOnlyCollection<string>> model) =>
        Ihlaller(model, [Web], VeriErisimi);

    public static IReadOnlyList<string> YB6_OrtakBasvurulari(IReadOnlyDictionary<string, IReadOnlyCollection<string>> model) =>
        Ihlaller(model, [Ortak], b => PolsanIzinProjesi(b) || VeriErisimi(b) || AspNetCore(b));

    private static List<string> Ihlaller(
        IReadOnlyDictionary<string, IReadOnlyCollection<string>> model,
        IEnumerable<string> projeler,
        Func<string, bool> yasak)
    {
        ArgumentNullException.ThrowIfNull(model);
        return projeler
            .Where(model.ContainsKey)
            .SelectMany(p => model[p].Where(yasak).Select(b => $"{p} -> {b}"))
            .ToList();
    }

    private static bool PolsanIzinProjesi(string ad) => ad.StartsWith("PolsanIzin.", StringComparison.Ordinal);

    private static bool AspNetCore(string ad) =>
        ad.StartsWith("Microsoft.AspNetCore", StringComparison.Ordinal);

    private static bool VeriErisimi(string ad) =>
        ad.StartsWith("Microsoft.EntityFrameworkCore", StringComparison.Ordinal)
        || ad.StartsWith("Microsoft.Data.Sqlite", StringComparison.Ordinal)
        || ad.StartsWith("SQLitePCLRaw", StringComparison.Ordinal);
}
