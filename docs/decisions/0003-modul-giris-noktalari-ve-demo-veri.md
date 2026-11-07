# ADR 0003 — Modül giriş noktaları ve demo veri

- Status: Accepted
- Date: 2026-10-21
- Revised: 2026-11-07 — madde 3'ün gerekçesi olgusal olarak eksikti: paket geçişli iki
  `Microsoft.AspNetCore.Cryptography.*` assembly'si getirir (review 0002 R-3). Karar değişmedi.

## Context
Spec 0002, demo verinin (çalışanlar, roller, resmi tatiller, izin hakları) yalnızca `Development`
ortamında yüklenmesini istiyor. `docs/architecture.md` her modüle iki public giriş tanıyor:
`Kaydet(IServiceCollection, string)` ve `SemaKur(IServiceProvider)`. `Web` EF Core'a dokunamıyor
(YB-5), yaprak modüller birbirini çağıramıyor (YB-3), bu yüzden:
- Demo veriyi kimin, hangi ortam bilgisiyle yükleyeceği tanımlı değil (modüller
  `Microsoft.AspNetCore.*` ve dolayısıyla `IWebHostEnvironment` göremez — YB-4).
- `Bakiyeler`'in izin hakları çalışan kimliklerine bağlı; `Bakiyeler` ise `Calisanlar`'ı bilemez.

Ek olarak iki araç kararı mimari testlerin yorumunu etkiliyor: parola hash'i için
`PasswordHasher<T>` ve modül testlerinin `internal` tiplere erişimi.

## Decision
1. `SemaKur` imzası `SemaKur(IServiceProvider, bool demoVeriYukle)` olur. `Web`, ikinci parametreyi
   `IsDevelopment()` ile verir. Giriş noktası sayısı iki kalır.
2. Her modül kendi demo verisini kendi içinde (`internal`) tutar ve yükler. Demo çalışanların
   kimlikleri **sabit Guid'lerdir**; `Calisanlar` ve `Bakiyeler`'de tekrarlanır. Tutarlılığı bir
   entegrasyon testi doğrular (her izin hakkı bir demo çalışana bağlı). Yükleme idempotenttir:
   kayıt varsa eklenmez.
3. Parolalar `Microsoft.Extensions.Identity.Core` paketindeki `PasswordHasher<T>` ile hash'lenir.
   Tip `Microsoft.AspNetCore.Identity` namespace'inde olsa da assembly'si `Microsoft.Extensions.*`
   olduğundan YB-4'ü (assembly adına bakar) doğrudan bozmaz. Paket geçişli olarak
   `Microsoft.AspNetCore.Cryptography.Internal` ve `.KeyDerivation`'ı getirir: bunlar HTTP
   pipeline değil, PBKDF2 kriptografi kütüphaneleridir; YB-4'ün amacını bozmadıkları için kabul
   edilir. YB-4 testi geçişli kapanışı da tarar ve yalnızca `Microsoft.AspNetCore.Cryptography.*`'ye
   izin verir.
4. Her modül, `InternalsVisibleTo` ile yalnızca **kendi** test projesine açılır. YB-1 modüller
   arası erişimi yasaklar; bir modülün kendi testinin erişimi bu kapsamın dışındadır. Bu kural
   `Mimari.Tests` içinde testle korunur.

## Consequences
- Kazanç: YB-3, YB-4 ve YB-5 gevşemez; `Web` demo veri için orkestrasyon veya iş kuralı yazmaz;
  modül sınırları ve veri sahipliği aynen kalır.
- Maliyet: demo çalışan Guid'leri iki yerde tekrarlanır (yalnızca demo verisi; domain mantığı
  değil) — tutarlılık derleyiciyle değil testle korunur. `SemaKur` imzası tüm modüllerde bir
  parametre taşır; demo verisi olmayan modül onu yok sayar.

## Alternatives considered
- **Her modüle üçüncü bir public giriş (`DemoVeriYukle`):** "iki giriş" kuralını genişletir ve
  YB-5 istisnasını büyütür; kazancı yalnızca isimlendirme.
- **`Web` orkestrasyonu** (Calisanlar'dan kimlikleri alıp `Bakiyeler`'e hak tanımlatmak): `Web`'e
  iş akışı koyar ("Web iş kuralı içermez") ve İK işlevi olan hak tanımlamayı erkenden public API
  yapar.
- **Demo kimliklerini `Ortak`'ta paylaşmak:** `Ortak`'a domain verisi sokar (YB-6).
- **Kendi PBKDF2 kodumuz:** güvenlik kodunu elle yazmak; resmi ve bakımı süren tip varken gereksiz risk.

## Revisit triggers
- Demo veri modüller arası daha fazla ilişki gerektirirse (ör. demo izin talepleri).
- Ortam bilgisi `bool`'dan fazlasını gerektirirse (ör. `Staging` için ayrı veri).
- Bir rol / çalışan yönetim ekranı gelirse demo verinin yerini gerçek bir ilk kurulum akışı alabilir.
