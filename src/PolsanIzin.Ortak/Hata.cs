namespace PolsanIzin.Ortak;

/// <summary>
/// Bir iş kuralı ihlali. <paramref name="Kod"/> kural kodudur (ör. "BR-2"); <paramref name="Alan"/>
/// verilirse ekran mesajı o alanın yanında gösterir.
/// </summary>
public sealed record Hata(string Kod, string Mesaj, string? Alan = null);
