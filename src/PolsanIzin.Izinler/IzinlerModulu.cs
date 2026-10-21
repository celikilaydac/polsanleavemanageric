using Microsoft.Extensions.DependencyInjection;

namespace PolsanIzin.Izinler;

/// <summary>
/// Modülün iki public girişi (docs/architecture.md, ADR 0003). Modül henüz veri sahibi değildir;
/// izin talebi verisi spec 0001 ile gelir. Girişler, Web'in tüm modülleri aynı biçimde kurması için
/// bugünden vardır.
/// </summary>
public static class IzinlerModulu
{
    public static void Kaydet(IServiceCollection services, string baglantiDizesi)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(baglantiDizesi);
    }

    public static void SemaKur(IServiceProvider services, bool demoVeriYukle)
    {
        ArgumentNullException.ThrowIfNull(services);
    }
}
