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
        var publicVeriTipleri = BagimlilikModeli.Assemblyler
            .SelectMany(a => a.GetTypes())
            .Where(t => t.IsVisible && (DbContextTuruMu(t) || VeriNamespaceindeMi(t)))
            .Select(t => t.FullName)
            .ToList();

        Assert.Empty(publicVeriTipleri);
    }

    [Fact]
    public void YB1_InternalsVisibleTo_YalnizcaKendiTestProjesine()
    {
        var ihlaller = BagimlilikModeli.Assemblyler
            .SelectMany(a => a.GetCustomAttributes<InternalsVisibleToAttribute>()
                .Where(i => i.AssemblyName != a.GetName().Name + ".Tests")
                .Select(i => $"{a.GetName().Name} -> {i.AssemblyName}"))
            .ToList();

        Assert.Empty(ihlaller);
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
