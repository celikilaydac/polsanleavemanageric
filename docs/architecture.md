# Architecture

## System overview
PolsanIzin, Polsan çalışanlarının izin taleplerini oluşturduğu, yöneticilerin onaylayıp
reddettiği ve İK'nın bakiye ve talepleri izlediği bir web uygulamasıdır. Eğitim projesidir;
gerçek kişisel veri kullanılmaz, hedef ölçek 50–500 kullanıcıdır. Mimari tarz **modüler
monolittir**: tek uygulama, tek SQLite veritabanı, içinde derleyici ve mimari testlerle korunan
modül sınırları ([ADR 0001](decisions/0001-moduler-monolit.md)). Teknoloji: .NET 10, ASP.NET Core
Razor Pages, EF Core + SQLite ([ADR 0002](decisions/0002-teknoloji-yigini.md)).

### Solution yapısı
```
PolsanIzin.slnx
src/
  PolsanIzin.Ortak/         class library — paylaşılan çekirdek (Sonuc, Hata, Bugun())
  PolsanIzin.Calisanlar/    class library
  PolsanIzin.Takvim/        class library
  PolsanIzin.Bakiyeler/     class library
  PolsanIzin.Izinler/       class library
  PolsanIzin.Web/           Razor Pages — giriş noktası, composition root
tests/
  PolsanIzin.<Modul>.Tests/ her modüle bir birim test projesi
  PolsanIzin.Mimari.Tests/  mimari kural testleri (YB-1…YB-6)
  PolsanIzin.Web.Tests/     entegrasyon testleri (WebApplicationFactory)
```

## Modules / components and ownership

| Module | Single responsibility | Owns |
|---|---|---|
| `Calisanlar` | Çalışan kimliği, roller (Çalışan / Yönetici / İK; BR-16), çalışan → yönetici bağı, kimlik bilgisi doğrulama | Çalışan, rol, yönetici bağı, parola hash'i |
| `Takvim` | İş günü hesabı ve resmi tatiller (BR-1) | Resmi tatil listesi |
| `Bakiyeler` | İzin hakkı ve bakiye; rezerve / kesinleştir / serbest bırak (BR-2, BR-9, BR-10, BR-11, BR-15); bakiye asla negatif olmaz | İzin hakkı, bakiye hareketleri |
| `Izinler` | İzin talebinin yaşam döngüsü: oluştur, onayla, reddet, iptal (BR-2…BR-9, BR-11, BR-13, BR-14); denetim izi | İzin talebi, durum geçmişi |
| `Web` | Razor Pages ekranları, kimlik doğrulama (cookie), yetki policy'leri; **iş kuralı içermez** | (veri sahibi değil) |
| `Ortak` | Paylaşılan çekirdek: `Sonuc` / `Sonuc<T>` / `Hata`, `TimeProvider.Bugun()` (BR-12). Domain değil, altyapı tipidir | (veri sahibi değil) |

## Communication rules
1. Modüller arası iletişim **yalnızca public arayüz** üzerinden (ör. `IBakiyeServisi`), senkron
   doğrudan çağrıdır. MVP'de olay/mesaj kuyruğu yoktur.
2. Bağımlılık yönü tek yönlüdür, döngü yoktur:
   ```
   Web ──► Izinler ──► Calisanlar
    │         ├──────► Takvim
    │         └──────► Bakiyeler
    └──► (tüm modüllerin yalnızca public arayüzleri)

   Tüm projeler ──► Ortak   (Ortak hiçbir projeye referans vermez)
   ```
3. Bakiye akışı: talep oluşturulunca `Izinler` → `Bakiyeler.Rezerve(...)`; onayda
   `Kesinlestir(...)`; ret veya iptalde `SerbestBirak(...)`. "Bakiye negatif olamaz" değişmezi
   yalnızca `Bakiyeler` içinde korunur.
4. Her modülün **kendi `DbContext`'i ve kendi tabloları** vardır (aynı SQLite dosyası); ortak
   DbContext yoktur. Her modül iki public giriş sunar; `Web` yalnızca bunları çağırır (YB-5):
   - `Kaydet(IServiceCollection, string baglantiDizesi)` — DI kaydı; tüm modüller **aynı bağlantı
     dizesini** alır, her DbContext kendi bağlantısını açar (bağlantı örneği paylaşılmaz —
     `SqliteConnection` thread-safe değildir; `Web` ADO.NET/SQLite paketine bağımlı olmaz).
     Üretimde dosya; testte paylaşılan adlandırılmış in-memory veritabanı
     (`Data Source=polsanizin-<guid>;Mode=Memory;Cache=Shared`), test factory'si veritabanı
     yaşasın diye bir keep-alive bağlantıyı açık tutar.
   - `SemaKur(IServiceProvider, bool demoVeriYukle)` — modülün şemasını `Database.Migrate()` ile
     kurar; her modül kendi geçmiş tablosunu kullanır
     (`MigrationsHistoryTable("__EFMigrationsHistory_<Modul>")`). `EnsureCreated` yasaktır
     (paylaşılan veritabanında ilk modülden sonra tablo oluşturmaz). `demoVeriYukle` yalnızca
     `Development`'ta `true`dur; modül kendi demo verisini idempotent yükler; demo çalışan
     kimlikleri sabit Guid'lerdir ([ADR 0003](decisions/0003-modul-giris-noktalari-ve-demo-veri.md)).
     `Web` EF Core'a doğrudan dokunmaz.
5. `Web` yalnızca modül servislerini çağırır; iş kuralı, doğrulama mantığı veya sorgu yazmaz.

## Forbidden dependencies (make them testable)

| Kod | Yasak | Koruma |
|---|---|---|
| YB-1 | Bir modül başka bir modülün `internal` tiplerine, entity'lerine veya DbContext'ine erişemez. | Entity ve DbContext'ler `internal` — derleyici |
| YB-2 | Proje referansları yukarıdaki grafiğin dışına çıkamaz; döngü yasak. | csproj referansları + `Mimari.Tests` |
| YB-3 | `Calisanlar`, `Takvim`, `Bakiyeler` `Ortak` dışında hiçbir projeye referans veremez. | `Mimari.Tests` |
| YB-4 | Modül projeleri `Microsoft.AspNetCore.*` assembly'lerine bağımlı olamaz. | `Mimari.Tests` |
| YB-5 | `Web`, EF Core / `DbContext`'e doğrudan erişemez (istisna: modüllerin `Kaydet(...)` ve `SemaKur(...)` çağrıları). | `Mimari.Tests` |
| YB-6 | `Ortak` hiçbir PolsanIzin projesine referans veremez; EF Core, `Microsoft.AspNetCore.*` ve domain tipi (entity, iş kuralı) içeremez. | `Mimari.Tests` + review |

Mimari testler ek paket olmadan reflection ile (`Assembly.GetReferencedAssemblies()`) yazılır.

## Deliberately out of scope
Mikroservisler · mesaj kuyruğu / olay altyapısı · dağıtık önbellek · çoklu kiracı (multi-tenant) ·
bordro / SGK entegrasyonu · kurumsal SSO (AD / Entra ID) · mobil uygulama · çok seviyeli onay ·
tüm bildirimler (e-posta dahil; yönetici bekleyen talepleri ekranda görür, gerekirse ileride
`Bildirimler` modülü ADR ile eklenir) · tarayıcı tabanlı E2E testleri (bkz. `docs/testing.md`).
