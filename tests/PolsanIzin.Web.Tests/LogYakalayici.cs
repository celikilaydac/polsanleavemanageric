using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace PolsanIzin.Web.Tests;

/// <summary>Uygulamanın yazdığı tüm log kayıtlarını (her seviye) metin olarak toplar (AC-16).</summary>
public sealed class LogYakalayici : ILoggerProvider
{
    private readonly ConcurrentQueue<string> _kayitlar = new();

    public IReadOnlyCollection<string> Kayitlar => _kayitlar;

    public ILogger CreateLogger(string categoryName) => new Kaydedici(categoryName, _kayitlar);

    public void Dispose()
    {
    }

    private sealed class Kaydedici(string kategori, ConcurrentQueue<string> kayitlar) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            // Hem biçimlenmiş mesaj hem ham durum değerleri: şablon dışı bir parametre de yakalansın.
            var degerler = state is IEnumerable<KeyValuePair<string, object?>> ciftler
                ? string.Join(" | ", ciftler.Select(c => $"{c.Key}={c.Value}"))
                : string.Empty;
            kayitlar.Enqueue($"{logLevel} {kategori}: {formatter(state, exception)} [{degerler}] {exception}");
        }
    }
}
