using System.Reflection;
using System.Text.Json;
using System.Xml.Linq;

namespace PolsanIzin.Mimari.Tests;

/// <summary>Gerçek bağımlılık modelleri: derlenmiş assembly başvuruları ve csproj başvuruları.</summary>
internal static class BagimlilikModeli
{
    public static readonly Assembly[] Assemblyler =
    [
        .. new[]
        {
            MimariKurallar.Ortak,
            MimariKurallar.Calisanlar,
            MimariKurallar.Takvim,
            MimariKurallar.Bakiyeler,
            MimariKurallar.Izinler,
            MimariKurallar.Web,
        }.Select(Assembly.Load),
    ];

    /// <summary>Derlenmiş IL'in gerçekten kullandığı assembly'ler (Assembly.GetReferencedAssemblies).</summary>
    public static IReadOnlyDictionary<string, IReadOnlyCollection<string>> AssemblyBasvurulari() =>
        Assemblyler.ToDictionary(
            a => a.GetName().Name!,
            a => (IReadOnlyCollection<string>)a.GetReferencedAssemblies().Select(r => r.Name!).ToArray());

    /// <summary>
    /// csproj'lardaki ProjectReference, PackageReference ve FrameworkReference adları — kullanılmayan
    /// bir başvuru IL'de görünmez, ama grafiği yine de bozar.
    /// </summary>
    public static IReadOnlyDictionary<string, IReadOnlyCollection<string>> ProjeBasvurulari()
    {
        var src = Path.Combine(DepoKoku(), "src");
        return Directory.GetFiles(src, "*.csproj", SearchOption.AllDirectories).ToDictionary(
            p => Path.GetFileNameWithoutExtension(p),
            p => (IReadOnlyCollection<string>)XDocument.Load(p).Descendants()
                .Where(e => e.Name.LocalName is "ProjectReference" or "PackageReference" or "FrameworkReference")
                .Select(BasvuruAdi)
                .ToArray());
    }

    /// <summary>
    /// Her projenin geçişli paket kapanışı: restore'un yazdığı <c>obj/project.assets.json</c>
    /// hedefindeki tüm kütüphaneler. Test projesinin deps.json'u işe yaramaz: <c>Web</c>'in
    /// <c>Microsoft.AspNetCore.App</c> framework'ü oradaki AspNetCore paketlerini budar.
    /// </summary>
    public static IReadOnlyDictionary<string, IReadOnlyCollection<string>> GecisliPaketler()
    {
        var src = Path.Combine(DepoKoku(), "src");
        return Directory.GetFiles(src, "*.csproj", SearchOption.AllDirectories).ToDictionary(
            p => Path.GetFileNameWithoutExtension(p),
            p =>
            {
                var assets = Path.Combine(Path.GetDirectoryName(p)!, "obj", "project.assets.json");
                using var belge = JsonDocument.Parse(File.ReadAllText(assets));
                return (IReadOnlyCollection<string>)belge.RootElement.GetProperty("targets")
                    .EnumerateObject()
                    .SelectMany(hedef => hedef.Value.EnumerateObject())
                    .Select(kutuphane => kutuphane.Name.Split('/')[0])
                    .Distinct()
                    .ToArray();
            });
    }

    private static string BasvuruAdi(XElement e)
    {
        var include = e.Attribute("Include")!.Value;
        return e.Name.LocalName == "ProjectReference"
            ? Path.GetFileNameWithoutExtension(include.Replace('\\', '/'))
            : include;
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
}
