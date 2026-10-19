# ADR 0002 — Teknoloji yığını: .NET 10 + Razor Pages + EF Core / SQLite

- Status: Accepted
- Date: 2026-10-19

## Context
Geliştirme makinesinde .NET SDK 10.0.402 ve Node.js 24 kurulu; Docker yok, Java sürümü eski.
Uygulama form ve liste ağırlıklıdır (talep oluştur, onayla, listele); zengin istemci etkileşimi
gerekmiyor. Eğitim projesi olduğu için hareketli parça sayısı az, test zinciri tek olmalıdır.

## Decision
Uygulama .NET 10 üzerinde **ASP.NET Core Razor Pages** (sunucu taraflı HTML) ile yazılır; veri
erişimi **EF Core + SQLite**, testler **xUnit v3** ile yapılır; SDK `global.json` ile sabitlenir.

## Consequences
- Kazanç: tek dil (C#), tek build ve tek test zinciri; Docker veya ayrı veritabanı sunucusu
  gerekmez; ekran davranışları tarayıcısız entegrasyon testleriyle (`WebApplicationFactory`)
  doğrulanabilir.
- Maliyet: SPA kadar akıcı bir arayüz yoktur; SQLite eşzamanlı yazmada sınırlıdır (bu ölçekte
  kabul edilebilir); .NET derleme araçlarının arka plan süreçleri (MSBuild node'ları, derleyici
  sunucusu) `scripts/check` içinde kapatılmalıdır (`docs/testing.md`, Resource hygiene).

## Alternatives considered
- **.NET Web API + React:** iki dil, iki build, iki test zinciri — eğitim projesi için gereksiz yük.
- **NestJS + React:** tamamen TypeScript; ekip ve kurum .NET ağırlıklı olduğu için daha az uygun.
- **Blazor Server:** C# ile etkileşimli UI, ancak SignalR bağlantısı ve bileşen testleri ek
  karmaşıklık getirir.

## Revisit triggers
- JS ağırlıklı, zengin etkileşimli ekran ihtiyacı (Blazor / SPA ve Playwright ADR'si).
- Eşzamanlı yazma veya veri hacmi SQLite'ı zorlarsa (PostgreSQL / SQL Server ADR'si).
