# Security

> Her plan ve her review'da uyulması zorunlu temel kurallar. Eğitim projesidir, gerçek kişisel veri
> yoktur — ama alışkanlıklar gerçek bir sistemdeki gibi kurulur.

## Secrets
- Secrets never enter the repo, specs, prompts, or chat. Yerel secret'lar `dotnet user-secrets` veya gitignore'daki `appsettings.*.local.json` içinde tutulur (bu projede `.env` kullanılmaz).
- Agents never print secret values, even when debugging.
- `appsettings.*.local.json` gitignore'dadır. SQLite bağlantı dizesi secret sayılmaz.
- Demo kullanıcı parolaları yalnızca `Development` seed verisinde bulunur; başka ortamda seed çalışmaz.

## Input & output
- Doğrulama iki katmanlıdır: arayüzde model doğrulaması (kullanıcı deneyimi için), modül
  servisinde iş kuralı doğrulaması (güvenlik ve doğruluk için). Sunucu tarafı doğrulama asla
  atlanmaz.
- Tüm POST formlarında antiforgery token zorunludur (Razor Pages varsayılanı; kapatılmaz).
- Çıktı Razor tarafından encode edilir. `Html.Raw` yalnızca gerekçeli istisnadır ve review'da
  sorgulanır.
- Ham SQL yoktur: `FromSqlRaw` / `ExecuteSqlRaw` ve string birleştirmeyle sorgu yasaktır; EF LINQ
  veya parametreli sorgu kullanılır.
- Hata yanıtlarında stack trace, SQL, iç tip adı yoktur; developer exception page yalnızca
  `Development` ortamında açıktır. `Development` dışında HTTPS yönlendirme ve HSTS açıktır.
- Loglara kişisel veri yazılmaz (ad, e-posta, ret gerekçesi); yalnızca ID'ler loglanır.

## AuthN / AuthZ
- **AuthN:** cookie tabanlı oturum; kimlik bilgisi `Calisanlar` modülünde `PasswordHasher` ile
  hash'lenmiş parolaya karşı doğrulanır. Düz metin parola hiçbir yerde saklanmaz veya loglanmaz.
  Kurumsal SSO kapsam dışıdır (gelirse ADR ile).
- **Kaba kuvvet koruması:** giriş POST'u ASP.NET Core yerleşik rate limiter ile IP başına
  dakikada 10 denemeyle sınırlıdır; aşımda 429 ve genel Türkçe mesaj döner. Hesap kilitleme MVP
  kapsamı dışıdır.
  - Limit options ile yapılandırılır (sabit kodlanmaz); entegrasyon test factory'si override eder.
  - IP'si `null` olan istekler (ör. TestServer) için açık bir bölüm anahtarı tanımlanır.
  - Ters vekil arkasına çıkılırsa önce `ForwardedHeaders` yapılandırılır; yoksa tüm kullanıcılar
    tek IP'yi paylaşır.
- **Varsayılan reddet:** global `FallbackPolicy = RequireAuthenticatedUser`. Anonim erişim yalnızca
  giriş sayfası ve statik dosyalar içindir.
- **Rol yetkisi:** `Calisan`, `Yonetici`, `Ik` rolleri sayfa seviyesinde policy ile korunur.
- **Kaynak yetkisi modül servisinde de uygulanır**, yalnızca ekranda değil: "onaylayan, çalışanın
  yöneticisi mi?" (BR-6), "talep bu çalışanın mı?". Gizlenmiş bir buton yetki değildir; doğrudan
  gönderilen istek de reddedilir.
- ID'ler tahmin edilemez (Guid v7) ama güvenlik buna dayanmaz; her erişimde yetki kontrol edilir.

## Dependencies
- Yeni NuGet paketi yalnızca planda gerekçesiyle yer alır ve plan onaylanırsa eklenir.
- Tercih sırası: Microsoft resmi paketleri → yaygın, bakımı süren açık kaynak paketler. Lisans
  MIT / Apache-2.0 / BSD uyumlu olmalıdır.
- NuGetAudit açıktır; bilinen zafiyetli paket (NU1901–NU1904) `TreatWarningsAsErrors` ile derlemeyi
  kırar. Düşük önem (NU1901) dahil — bilinçli seçim: eğitim projesinde katı varsayılan;
  CI bir advisory ile kırmızıya dönerse paket yükseltilir (`chore/`), seviye düşürülmez.

## Review lens
Security is a mandatory dimension of every independent review (see `prompts/review.md`), not a
separate afterthought phase.
