# ADR 0004 — Talep oluşturma tutarlılığı (Izinler ↔ Bakiyeler)

- Status: Accepted
- Date: 2026-11-07

## Context
Spec 0001'de bir izin talebi oluşturulurken iki modül yazar: `Izinler` talebi ve denetim kaydını,
`Bakiyeler` (yalnızca `Yillik` için) rezerveyi. Her modülün kendi `DbContext`'i vardır ve ortak
transaction yoktur (`docs/architecture.md`, kural 4). Ayrıca iki kural "kontrol et → yaz"
yarışına açıktır: çakışma (BR-3; aynı formun iki kez gönderilmesi, AC-19) ve bakiye (BR-2; aynı
anda iki `Yillik` talep, AC-23). Bakiye hiçbir koşulda negatif olmamalıdır (R-9). Uygulama tek
süreç ve tek SQLite dosyasıyla çalışır; hedef ölçek 50–500 kullanıcıdır.

## Decision
1. `IzinTalebiServisi.TalepOlusturAsync` akışın tamamını **süreç içi tek bir kilitle**
   (`SemaphoreSlim(1, 1)`) seri hâle getirir.
2. Sıra: alan kuralları → BR-3 + BR-2 kontrolü → `Bakiyeler.RezerveAsync` → talep + denetim kaydı
   (aynı `IzinlerDbContext`, tek `SaveChanges`) → yazım başarısızsa `Bakiyeler.RezerveGeriAlAsync`
   (telafi) ve exception yeniden fırlatılır.
3. `Bakiyeler.RezerveAsync` bakiyeyi kendi içinde, kendi kilidi altında yeniden kontrol eder:
   "bakiye negatif olamaz" değişmezinin sahibi `Bakiyeler`'dir; `Izinler`'in kilidi olmasa da
   korunur.

## Consequences
- Kazanç: AC-19 ve AC-23 deterministik; değişmez tek modülde korunur; modül sınırı (ayrı
  `DbContext`) bozulmaz.
- Maliyet: talep oluşturma süreç içinde seri çalışır (hedef ölçekte ihmal edilebilir).
- Kabul edilen artık risk: süreç, rezerve ile talep yazımı arasında çökerse yetim bir rezerve
  kalır (telafi çalışmaz). Tespit ve temizlik ileride.
- Çoklu örnek (multi-instance) dağıtım bu kararla desteklenmez.

## Alternatives considered
- **SQLite `BEGIN IMMEDIATE` transaction'ları:** iki ayrı bağlantıda iç içe kullanılırsa kendi
  kendini kilitler; testteki shared-cache in-memory modunda `SQLITE_LOCKED` beklemeden hata verir.
- **Ortak `DbContext` / dağıtık transaction:** modül sınırını ve veri sahipliğini bozar (YB-1).
- **Yalnızca veritabanı kısıtları:** BR-3 (aralık kesişimi) ve BR-2 (toplam üzerinden eşik) basit
  bir kısıtla ifade edilemez.

## Revisit triggers
- Uygulama birden fazla örnekle çalıştırılırsa.
- Yetim rezerve gerçek kullanımda görülürse.
- Onay / ret / iptal akışları (Kesinleştir, Serbest bırak) aynı yarışları getirirse.
