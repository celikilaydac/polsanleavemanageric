using System.Reflection;
using System.Runtime.CompilerServices;

namespace PolsanIzin.Mimari.Tests;

/// <summary>YB-1…YB-6 gerçek assembly'ler ve csproj'lar üzerinde (docs/architecture.md).</summary>
public class MimariTests
{
    public static TheoryData<string> Modeller => ["assembly", "csproj"];

    private static IReadOnlyDictionary<string, IReadOnlyCollection<string>> Model(string tur) =>
        tur == "assembly" ? BagimlilikModeli.AssemblyBasvurulari() : BagimlilikModeli.ProjeBasvurulari();

    [Fact]
    public void YB1_DbContextVeEntityler_PublicDegil()
    {
        var tipler = BagimlilikModeli.Assemblyler.SelectMany(a => a.GetTypes()).ToList();
        var entityler = tipler
            .Where(DbContextTuruMu)
            .SelectMany(c => c.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            .Select(p => p.PropertyType)
            .Where(t => t.IsGenericType && t.GetGenericTypeDefinition().FullName == "Microsoft.EntityFrameworkCore.DbSet`1")
            .Select(t => t.GetGenericArguments()[0])
            .ToHashSet();

        // Model gerçekten entity'leri buluyor: aksi halde kural boş girdiyle sessizce geçerdi.
        Assert.NotEmpty(entityler);
        Assert.Empty(MimariKurallar.YB1_PublicVeriTipleri(tipler.Select(t => new MimariKurallar.TipBilgisi(
            t.FullName!, t.IsVisible, DbContextTuruMu(t), entityler.Contains(t), VeriNamespaceindeMi(t)))));
    }

    [Fact]
    public void YB1_InternalsVisibleTo_YalnizcaKendiTestProjesine()
    {
        var acilimlar = BagimlilikModeli.Assemblyler.SelectMany(a => a.GetCustomAttributes<InternalsVisibleToAttribute>()
            .Select(i => (a.GetName().Name!, i.AssemblyName)));

        Assert.Empty(MimariKurallar.YB1_InternalsVisibleToIhlalleri(acilimlar));
    }

    [Theory]
    [MemberData(nameof(Modeller))]
    public void YB2_ProjeGrafi_IzinliKenarlarDisinaCikmaz(string model)
    {
        Assert.Empty(MimariKurallar.YB2_GrafDisiBasvurular(Model(model)));
    }

    [Theory]
    [MemberData(nameof(Modeller))]
    public void YB3_YaprakModuller_YalnizcaOrtagaBasvurur(string model)
    {
        Assert.Empty(MimariKurallar.YB3_YaprakModulBasvurulari(Model(model)));
    }

    [Theory]
    [MemberData(nameof(Modeller))]
    public void YB4_ModulProjeleri_AspNetCoreaBagimliDegil(string model)
    {
        Assert.Empty(MimariKurallar.YB4_AspNetCoreBasvurulari(Model(model)));
    }

    [Fact]
    public void YB4_ModulProjeleri_GecisliOlarakDaAspNetCoreaBagimliDegil()
    {
        var gecisli = BagimlilikModeli.GecisliPaketler();

        // Model gerçekten geçişli kapanışı okuyor: parola hash'inin getirdiği kriptografi paketi görünür.
        Assert.Contains("Microsoft.AspNetCore.Cryptography.KeyDerivation", gecisli[MimariKurallar.Calisanlar]);
        Assert.Empty(MimariKurallar.YB4_GecisliAspNetCoreBagimliliklari(gecisli));
    }

    [Theory]
    [MemberData(nameof(Modeller))]
    public void YB5_Web_EfCoreVeSqliteaDogrudanErismez(string model)
    {
        Assert.Empty(MimariKurallar.YB5_WebVeriErisimi(Model(model)));
    }

    [Fact]
    public void YB5_Web_DbContextTuretmez()
    {
        var webDbContextleri = typeof(Program).Assembly.GetTypes().Where(DbContextTuruMu).ToList();

        Assert.Empty(webDbContextleri);
    }

    [Theory]
    [MemberData(nameof(Modeller))]
    public void YB6_Ortak_HicbirProjeyeEfCoreVeAspNetCoreaBasvurmaz(string model)
    {
        Assert.Empty(MimariKurallar.YB6_OrtakBasvurulari(Model(model)));
    }

    [Fact]
    public void Modeller_TumProjeleriKapsar()
    {
        Assert.Equal(MimariKurallar.IzinliGraf.Keys.Order(), BagimlilikModeli.ProjeBasvurulari().Keys.Order());
        Assert.Equal(MimariKurallar.IzinliGraf.Keys.Order(), BagimlilikModeli.AssemblyBasvurulari().Keys.Order());
    }

    private static bool DbContextTuruMu(Type tur)
    {
        for (var t = tur.BaseType; t is not null; t = t.BaseType)
        {
            if (t.FullName == "Microsoft.EntityFrameworkCore.DbContext")
            {
                return true;
            }
        }

        return false;
    }

    private static bool VeriNamespaceindeMi(Type tur) =>
        tur.Namespace is { } ns
        && (ns.EndsWith(".Veri", StringComparison.Ordinal) || ns.Contains(".Veri.", StringComparison.Ordinal));
}
