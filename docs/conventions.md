# Conventions

> Yalnızca gerçek kurallar: her kural ya araçla korunur (tercih edilen) ya da review'da kontrol
> edilir.

## Language
<!-- Set at bootstrap (step 0) and mirrored in AGENTS.md. Changing it later is allowed — edit both
     places; existing documents are not translated retroactively. -->
- Chat language (interviews, sessions): Türkçe
- Document language (docs/, specs, plans, ADRs, review and verify reports): Türkçe
- Always English (protocol, not prose): ANEW core files, template headings and field labels,
  `Status:` values (Draft / Approved / In progress / Shipped), `Approved by / on:`, `Source:`.
- Code identifiers, branch names, commit messages: **Türkçe.** Tanımlayıcılar ASCII'dir
  (`ı ş ğ ü ö ç` → `i s g u o c`: `IzinTalebi`, `OnayVer()`); branch önekleri ve Conventional
  Commits türleri İngilizce kalır (`docs/git.md`); commit açıklamalarında Türkçe karakter serbesttir.
  Kullanıcıya görünen metinler doğru Türkçe karakterlerle yazılır.
- Agent messages (handoff summaries, refusals, `Next:` lines): chat language; command names, file
  paths and protocol fields verbatim.

## Language & framework versions
- .NET 10 (`net10.0`); SDK `global.json` ile sabitlenir (`10.0.402`, `rollForward: latestFeature`).
- ASP.NET Core Razor Pages, EF Core (SQLite sağlayıcısı), xUnit v3.
- Ortak derleme ayarları `Directory.Build.props` içinde: `Nullable=enable`,
  `ImplicitUsings=enable`, `TreatWarningsAsErrors=true`, `AnalysisLevel=latest-recommended`,
  `EnforceCodeStyleInBuild=true`.
- Paket sürümleri yalnızca `Directory.Packages.props` içinde (Central Package Management);
  csproj'larda sürüm yazılmaz.

## Naming
- C# standardı: tipler, metotlar, property'ler `PascalCase`; yerel değişken ve parametreler
  `camelCase`; private alanlar `_camelCase`; arayüzler `I` önekli (`IBakiyeServisi`).
- Domain terimleri `docs/domain.md` tablosundaki kod adlarıyla birebir kullanılır.
- Namespace = proje + klasör yolu (`PolsanIzin.Izinler.Talepler`).
- Test adları: `Metot_Durum_BeklenenSonuc` (ör. `TalepOlustur_BakiyeYetersiz_BR2HatasiDoner`).
- Branch ve commit adları: `docs/git.md`.

## Error handling
1. **İş kuralı ihlali exception değildir.** Modül servisleri `Sonuc` / `Sonuc<T>` döndürür; hata,
   kural koduyla gelir: `Hata("BR-2", "Yetersiz bakiye")`. Ekran kullanıcıya Türkçe mesajı gösterir.
2. **Exception yalnızca beklenmeyen durum içindir** (programlama hatası, DB erişim hatası). Global
   olarak yakalanır, `ILogger` ile loglanır, kullanıcı genel hata sayfasını görür.
3. Kullanıcıya asla stack trace, SQL veya iç tip adı gösterilmez.
4. Boş `catch` bloğu yasaktır; yakalanan exception ya loglanır ya da yeniden fırlatılır.

## Data rules
- İzin tarihleri `DateOnly`'dir (gün bazlı, saat dilimi yok).
- Zaman damgaları (denetim izi vb.) `DateTimeOffset`, UTC.
- "Şimdi" / "bugün" her zaman enjekte edilen `TimeProvider` ile alınır; `DateTime.Now`,
  `DateTime.UtcNow`, `DateTimeOffset.Now` doğrudan kullanılmaz.
- Gün sayıları `int`'tir (BR-11: yarım gün yok).
- Kimlikler `Guid`, `Guid.CreateVersion7()` ile üretilir.
- Enum'lar veritabanında string olarak saklanır.

## Enforced by tooling
- Derleyici + Roslyn analizörleri, uyarılar hata (`TreatWarningsAsErrors`), nullable kontrolü —
  `scripts/check` build adımı.
- Kod stili `.editorconfig` + `dotnet format --verify-no-changes` — `scripts/check` format adımı.
- Mimari sınırlar YB-1…YB-5 — derleyici (`internal`) + `PolsanIzin.Mimari.Tests`.
- Bilinen zafiyetli paketler — NuGetAudit (NU1901–NU1904) + `TreatWarningsAsErrors` derlemeyi kırar.
- Review'da kontrol edilenler (henüz araçla değil): `TimeProvider` kuralı, `Sonuc` deseni, Türkçe
  ASCII isimlendirme. Bir analizörle korunabildiği anda `scripts/check`'e taşınır.
