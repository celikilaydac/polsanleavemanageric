# Spec 0002 — Oturum açma ve seed veri

- Status: Shipped
- Mode: lite
- Plan: `specs/plans/0002-plan.md`

## Intent
PolsanIzin henüz çalışan bir uygulama değildir. Bu iş temeli kurar: `docs/architecture.md`'deki
modül yapısına uyan çalışır bir uygulama, çalışanların e-posta ve parolayla oturum açıp
kapatabilmesi ve geliştirme ortamında demo veri (çalışanlar, roller, yönetici bağları, resmi
tatiller, izin hakları). Başarı: seed edilmiş bir çalışan oturum açıp kendini tanıyan ana sayfayı
görür; oturumu olmayan kullanıcı her sayfadan giriş ekranına yönlendirilir; `scripts/check`
gerçek adımlarla (derleme, testler, mimari testler, format) yeşildir. Bilinçli olarak
yapılmayanlar: rol / çalışan yönetimi ekranları, parola sıfırlama / değiştirme, hesap kilitleme,
SSO, izin talebi işlevleri (spec 0001). Bu spec, spec 0001'in önkoşuludur.

## Requirements
**Uygulama temeli**
- **R-1** Uygulama `docs/architecture.md`'deki modül yapısına (`Ortak`, `Calisanlar`, `Takvim`,
  `Bakiyeler`, `Izinler`, `Web`) ve iletişim kurallarına uyar; yasaklı bağımlılıklar YB-1…YB-6
  otomatik testlerle korunur.
- **R-2** `scripts/check` gerçek adımlarla çalışır (`scripts/check.conf`'taki planlanmış adımlar
  etkin); hızlı (`--fast`) ve tam profilin test sayıları toplamı tüm testlere eşittir
  (`docs/testing.md`).
- **R-3** "Bugün" her yerde Europe/Istanbul takvim günüdür (BR-12); bu davranış uygulama
  genelinde tek bir yerden sağlanır ve testle doğrulanır.

**Oturum açma / kapama**
- **R-4** Çalışan e-posta ve parolasıyla oturum açar. E-posta büyük/küçük harf duyarsızdır ve
  baştaki/sondaki boşluklar yok sayılır.
- **R-5** Hatalı e-posta veya parolada, hesabın var olup olmadığını belli etmeyen tek bir mesaj
  gösterilir: "E-posta veya parola hatalı". Girilen e-posta korunur, parola alanı boşaltılır.
- **R-6** Aynı IP'den bir dakika içinde 10'dan fazla giriş denemesi yapılırsa sonraki denemeler
  reddedilir ve genel bir Türkçe "Çok fazla deneme, lütfen biraz sonra tekrar deneyin" mesajı
  gösterilir (`docs/security.md`). Pencere 1 dakikalık sabit penceredir. Hesap kilitlenmez.
- **R-7** Oturum, 8 saat hareketsizlikten sonra sona erer. "Beni hatırla" seçeneği yoktur.
- **R-8** Oturumu olmayan kullanıcı, giriş ekranı dışındaki her sayfadan giriş ekranına
  yönlendirilir; başarılı girişten sonra istediği sayfaya döner. Dönüş adresi uygulama dışını
  gösteriyorsa ana sayfaya gidilir.
- **R-9** Oturumu açık kullanıcı giriş ekranını açarsa ana sayfaya yönlendirilir.
- **R-10** Çalışan "Çıkış" ile oturumu kapatır ve giriş ekranına döner; çıkıştan sonra korumalı
  sayfalar yeniden giriş ister.
- **R-11** Parolalar hiçbir yerde düz metin saklanmaz veya loglanmaz; loglarda kişisel veri
  (ad, e-posta) yer almaz (`docs/security.md`).

**Ana sayfa**
- **R-12** Oturum açan çalışan ana sayfada "Hoş geldiniz, <ad soyad>" ve rollerini görür. Rol
  adları ekranda Türkçe karakterlerle gösterilir: "Çalışan", "Yönetici", "İK".
- **R-13** Her sayfada ortak bir menü vardır; menüde yalnızca var olan sayfalar ve "Çıkış"
  bulunur. Diğer menü öğelerini kendi spec'leri ekler.

**Seed veri (yalnızca Development)**
- **R-14** Seed veri yalnızca `Development` ortamında yüklenir; diğer ortamlarda hiçbir demo
  çalışan, parola, tatil veya hak oluşmaz.
- **R-15** Seed tekrar çalıştırıldığında (uygulama yeniden başlatıldığında) kayıt çoğalmaz.
- **R-16** Seed çalışanlar ve hiyerarşi (kurgusal adlar, `@polsan.example` e-postaları):

  | Kişi | Roller | Yöneticisi |
  |---|---|---|
  | Genel Müdür | Calisan, Yonetici | — (hiyerarşinin tepesi, BR-14) |
  | İK-1 | Calisan, Ik | Genel Müdür |
  | İK-2 | Calisan, Ik | Genel Müdür |
  | Yönetici A | Calisan, Yonetici | Genel Müdür |
  | Yönetici B | Calisan, Yonetici | Genel Müdür |
  | Çalışan A1 | Calisan | Yönetici A |
  | Çalışan A2 | Calisan | Yönetici A |
  | Çalışan B1 | Calisan | Yönetici B |

  Sistemde en az iki İK bulunur (BR-16); her çalışanın en fazla bir yöneticisi vardır;
  hiyerarşide döngü yoktur; kendisine bağlı çalışanı olan herkes `Yonetici` rolündedir.
- **R-17** Seed resmi tatiller: 2026 ve 2027 Türkiye resmi tatillerinin **tam günleri**;
  arife yarım günleri girilmez (BR-11). Liste:

  | Yıl | Tatiller |
  |---|---|
  | 2026 | 1 Oca · 20–22 Mar (Ramazan B.) · 23 Nis · 1 May · 19 May · 27–30 May (Kurban B.) · 15 Tem · 30 Ağu · 29 Eki |
  | 2027 | 1 Oca · 9–11 Mar (Ramazan B.) · 23 Nis · 1 May · 16–19 May (Kurban B.; 19 Mayıs ile çakışır) · 15 Tem · 30 Ağu · 29 Eki |

  2027 bayram tarihleri BUILD sırasında resmi kaynakla (Diyanet takvimi / Resmî Gazete)
  doğrulanır; fark çıkarsa bu liste R-08 ile güncellenir — sessizce değil.
- **R-18** Seed izin hakları: her seed çalışana **2026** için 14 gün; **2027 için hak tanımlanmaz**
  (spec 0001 AC-22 buna dayanır).

## User experience
- **Flow:** Kullanıcı herhangi bir adresi açar → oturumu yoksa giriş ekranı → e-posta + parola →
  "Giriş Yap" → istediği sayfa ya da ana sayfa → menüden "Çıkış" → giriş ekranı.
- **States:**
  - *Giriş — boş:* e-posta ve parola alanları boş.
  - *Giriş — hata:* R-5 mesajı formun üstünde; zorunlu alan boşsa alanın yanında mesaj; e-posta
    korunur.
  - *Giriş — çok fazla deneme:* R-6 mesajı; form yine görünür.
  - *Ana sayfa — başarı:* karşılama metni, roller, menü.
  - *Yetkisiz:* korumalı sayfa → giriş ekranına yönlendirme.
  - *Beklenmeyen hata:* genel hata sayfası; stack trace / iç ayrıntı yok.
  - *Yükleniyor:* ayrı bir durum yoktur (sunucu tarafında oluşturulan sayfalar).
- **Design reference:** none — the flow above is the design.
- **Accessibility:** giriş alanlarının görünür etiketleri vardır; form yalnızca klavyeyle
  (Tab / Enter) doldurulup gönderilir; hata mesajları ilgili alanla ilişkilendirilir; parola
  alanı gizli girilir.

## Constraints & out of scope
- Kimlik doğrulama, yetkilendirme, parola saklama ve giriş denemesi sınırı `docs/security.md`'ye
  uyar; antiforgery tüm formlarda açıktır (çıkış dahil).
- Demo parolaları spec'e yazılmaz. Tüm demo kullanıcılar tek bir demo parola kullanır; değer
  yalnızca `Development` seed verisinde bulunur. README'nin "Geliştirme" bölümü değeri tekrar
  etmeden seed verisinin yerini gösterir.
- R-14 uygulamanın çalışma zamanı içindir: otomatik testler kendi verilerini kendi test
  altyapısıyla oluşturur; AC-25 uygulama `Production` ortamında başlatılarak doğrulanır.
- `Development` dışındaki ortamda sistem boş açılır; ilk kullanıcıyı oluşturmanın yolu yoktur.
  Bu MVP'de bilinçli olarak kabul edilir.
- Kalıcı veritabanı SQLite dosyasıdır; şema uygulama açılışında kurulur.
- **Out of scope:** çalışan / rol / tatil / hak yönetim ekranları; parola sıfırlama ve
  değiştirme; hesap kilitleme; SSO; "beni hatırla"; çok dilli arayüz; izin talebi işlevleri.

## Acceptance criteria
**Uygulama temeli**
- [x] AC-1 — YB-1…YB-6'nın her biri için bir mimari test vardır ve geçer; kuralı bozan bir
  bağımlılık eklendiğinde ilgili test kırılır.
- [x] AC-2 — `./scripts/check` tam profili gerçek adımlarla yeşildir; `--fast` ve full-only test
  sayılarının toplamı tüm testlerin sayısına eşittir.
- [x] AC-3 — UTC 2026-10-21 21:30 anında "bugün" 2026-10-22, UTC 2026-10-21 20:59 anında
  2026-10-21'dir.
- [x] AC-4 — Uygulama boş bir veritabanıyla açıldığında tüm modüllerin şeması kurulur; ikinci
  açılışta hata vermez.

**Oturum açma / kapama**
- [x] AC-5 — Seed çalışan doğru e-posta + parolayla giriş yapar ve ana sayfaya ulaşır.
- [x] AC-6 — E-posta büyük harfli ve başında/sonunda boşlukla girildiğinde giriş başarılıdır.
- [x] AC-7 — Yanlış parola ve kayıtlı olmayan e-posta aynı mesajı ("E-posta veya parola hatalı")
  verir; e-posta korunur, parola boşalır; oturum açılmaz.
- [x] AC-8 — E-posta veya parola boş gönderildiğinde ilgili alanın yanında mesaj gösterilir;
  oturum açılmaz.
- [x] AC-9 — Aynı IP'den 1 dakikalık pencerede 10 deneme işlenir; 11. deneme R-6 mesajıyla
  reddedilir (doğru parolayla bile); sonraki pencerede deneme yeniden işlenir.
- [x] AC-10 — Oturumu olmayan kullanıcı ana sayfaya veya başka bir korumalı adrese gittiğinde
  giriş ekranına yönlendirilir; girişten sonra istediği adrese döner.
- [x] AC-11 — Dönüş adresi uygulama dışını gösterdiğinde (ör. `https://kotu.example`) giriş
  sonrası ana sayfaya gidilir.
- [x] AC-12 — Oturumu açık kullanıcı giriş ekranını açtığında ana sayfaya yönlendirilir.
- [x] AC-13 — "Çıkış" sonrası giriş ekranı görünür; ana sayfa yeniden açılınca giriş istenir.
- [x] AC-14 — Çıkış ve giriş istekleri antiforgery doğrulaması olmadan reddedilir.
- [x] AC-15 — Oturum 8 saat hareketsizlikten sonra geçersizdir; 8 saatten kısa aralıklarla
  kullanımda devam eder.
- [x] AC-16 — Saklanan parola düz metin değildir; başarılı ve başarısız giriş loglarında parola,
  ad veya e-posta geçmez.

**Ana sayfa ve menü**
- [x] AC-17 — Ana sayfa "Hoş geldiniz, <ad soyad>" ve kullanıcının rollerini Türkçe adlarıyla
  gösterir (ör. İK-1 için "Çalışan" ve "İK").
- [x] AC-18 — Menüde ana sayfa ve "Çıkış" vardır; var olmayan sayfaya bağlantı yoktur.
- [x] AC-19 — Beklenmeyen bir hata, `Development` dışında stack trace içermeyen genel hata
  sayfası gösterir.

**Seed veri**
- [x] AC-20 — `Development` ortamında R-16 tablosundaki 8 çalışan, rolleri ve yönetici bağları
  vardır; her biri giriş yapabilir.
- [x] AC-21 — Seed sonrası en az iki İK vardır (BR-16); hiçbir çalışanın birden fazla yöneticisi
  yoktur; hiyerarşide döngü yoktur; tam olarak bir kişinin (Genel Müdür) yöneticisi yoktur;
  kendisine bağlı çalışanı olan herkes `Yonetici` rolündedir.
- [x] AC-22 — Seed tatilleri R-17 listesiyle birebir aynıdır; arife günleri listede yoktur
  (ör. 2026-10-29 tatil; 2026-03-19 Ramazan Bayramı arifesi tatil değil).
- [x] AC-23 — Her seed çalışanın 2026 hakkı 14 gündür; 2027 için hiçbir çalışanın hakkı yoktur.
- [x] AC-24 — Uygulama iki kez açıldığında çalışan, tatil ve hak sayıları değişmez.
- [x] AC-25 — `Development` dışındaki ortamda açılan uygulamada hiçbir çalışan, tatil veya hak
  yoktur; seed e-postalarıyla giriş yapılamaz.

**Erişilebilirlik**
- [x] AC-26 — Giriş formunun alanlarının görünür etiketleri vardır, hata mesajları alana bağlıdır,
  parola alanı gizlidir ve form yalnızca klavyeyle doldurulup gönderilebilir.

## Definition of Done
- [x] Every acceptance criterion mapped to proof (test or reproducible observation)
- [x] `scripts/check` green
- [x] Independent review done; real findings fixed, noise rejected with written rationale
- [x] Docs / ADRs updated if behavior or architecture changed
- [x] Spec moved to `specs/done/` (it becomes immutable there)

## Scorecard (fill at ship — honest numbers make the process improvable)
| Metric | Value |
|---|---|
| Spec revisions | 0 (onaydan sonra spec değişmedi) |
| Fix rounds | 4 (review R, re-review F, VERIFY V, re-review H; son review J temiz) |
| Review findings: real / noise | 13 real / 2 noise; 3 takip işi (H-3, H-4, J-1 → spec 0003). VERIFY: 2 real (V-1 flaky, V-2) |
| Regressions introduced | 0 |
| Bugs escaped to production | 0 (henüz dağıtım yok) |
