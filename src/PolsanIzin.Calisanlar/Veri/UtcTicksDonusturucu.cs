using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace PolsanIzin.Calisanlar.Veri;

/// <summary>docs/conventions.md: <see cref="DateTimeOffset"/> UTC ticks (<c>long</c>) olarak saklanır.</summary>
internal sealed class UtcTicksDonusturucu()
    : ValueConverter<DateTimeOffset, long>(v => v.UtcTicks, v => new DateTimeOffset(v, TimeSpan.Zero));
