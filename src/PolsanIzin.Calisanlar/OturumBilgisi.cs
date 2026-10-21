namespace PolsanIzin.Calisanlar;

/// <summary>Kimliği doğrulanmış çalışanın oturuma yazılan bilgileri.</summary>
public sealed record OturumBilgisi(Guid CalisanId, string AdSoyad, IReadOnlyList<Rol> Roller);
