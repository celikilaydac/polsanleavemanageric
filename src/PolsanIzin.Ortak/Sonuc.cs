namespace PolsanIzin.Ortak;

/// <summary>
/// Bir modül işleminin sonucu. İş kuralı ihlali exception değildir (docs/conventions.md); başarısız
/// sonuç bir veya daha fazla <see cref="Hata"/> taşır. Değerli sonuçlar da buradaki fabrika
/// metotlarıyla üretilir: <c>Sonuc.Basari(deger)</c>, <c>Sonuc.Basarisiz&lt;T&gt;(hata)</c>.
/// </summary>
public class Sonuc
{
    private static readonly Sonuc _basarili = new([]);

    private protected Sonuc(IReadOnlyList<Hata> hatalar)
    {
        Hatalar = hatalar;
    }

    public IReadOnlyList<Hata> Hatalar { get; }

    public bool Basarili => Hatalar.Count == 0;

    public static Sonuc Basari() => _basarili;

    public static Sonuc Basarisiz(Hata hata, params Hata[] digerleri) => new(HatalariTopla(hata, digerleri));

    public static Sonuc Basarisiz(IEnumerable<Hata> hatalar) => new(HatalariDogrula(hatalar));

    public static Sonuc<T> Basari<T>(T deger) => new(deger, []);

    public static Sonuc<T> Basarisiz<T>(Hata hata, params Hata[] digerleri) => new(default, HatalariTopla(hata, digerleri));

    public static Sonuc<T> Basarisiz<T>(IEnumerable<Hata> hatalar) => new(default, HatalariDogrula(hatalar));

    private static Hata[] HatalariTopla(Hata hata, Hata[] digerleri)
    {
        ArgumentNullException.ThrowIfNull(hata);
        return [hata, .. digerleri];
    }

    private static Hata[] HatalariDogrula(IEnumerable<Hata> hatalar)
    {
        ArgumentNullException.ThrowIfNull(hatalar);
        var dizi = hatalar.ToArray();
        if (dizi.Length == 0)
        {
            throw new ArgumentException("Başarısız sonuç en az bir hata taşımalıdır.", nameof(hatalar));
        }

        return dizi;
    }
}

/// <summary>Değer taşıyan sonuç; değer yalnızca başarılı sonuçta okunabilir.</summary>
public sealed class Sonuc<T> : Sonuc
{
    private readonly T? _deger;

    internal Sonuc(T? deger, IReadOnlyList<Hata> hatalar)
        : base(hatalar)
    {
        _deger = deger;
    }

    public T Deger => Basarili
        ? _deger!
        : throw new InvalidOperationException("Başarısız sonucun değeri yoktur.");
}
