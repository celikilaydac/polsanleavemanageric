# Spec 0001 — İzin talebi giriş ekranı

- Status: Approved
- Mode: lite
- Plan: `specs/plans/0001-plan.md`

## Intent
Polsan çalışanları izin taleplerini bugün sistem dışında iletiyor. Bu iş, oturum açmış her
çalışanın kendi adına bir izin türü ve tarih aralığı seçerek izin talebi oluşturabildiği ekranı
sağlar. Talep `Beklemede` durumunda kaydedilir; `Yillik` ise iş günü sayısı kadar gün o yılın
bakiyesinden rezerve edilir. Kural ihlalinde (BR-1…BR-5, BR-11…BR-13) çalışan, talebin neden
alınmadığını alan bazında ve anlaşılır biçimde görür. Başarı: geçerli talep tek gönderimde doğru
iş günü sayısıyla kaydedilir; geçersiz talep hiçbir kayıt ve rezerve oluşturmaz.
Bilinçli olarak yapılmayanlar: onay / ret, iptal, talep listeleri ("Taleplerim"), İK ekranları,
bildirimler. Oturum açma ve seed veri (çalışanlar, roller, resmi tatiller, izin hakları) ayrı bir
spec'tir (CLARIFY S2) ve bu spec'in **önkoşuludur**.

## Requirements
- **R-1** Oturum açmış her çalışan (Yönetici ve İK rolündekiler dahil) izin talebi giriş ekranına
  erişebilir. Talep her zaman oturumdaki çalışan adına oluşturulur; başka bir çalışan adına talep
  açmanın hiçbir yolu yoktur (doğrudan gönderilen istekte de).
- **R-2** Form alanları:
  - İzin türü — zorunlu; `Yillik`, `Mazeret`, `Ucretsiz` seçeneklerinden biri (ekranda "Yıllık",
    "Mazeret", "Ücretsiz"). Varsayılan seçim yoktur.
  - Başlangıç tarihi — zorunlu, gün bazlı tarih.
  - Bitiş tarihi — zorunlu, gün bazlı tarih.
  - Açıklama — isteğe bağlı, en fazla 500 karakter.
- **R-3** Ekran, çalışanın **bugünün yılı** (BR-12) için kullanılabilir yıllık izin bakiyesini
  (hak − kesinleşen kullanım − rezerve) gün olarak gösterir. O yıl için izin hakkı tanımlı değilse
  "Bu yıl için izin hakkınız tanımlı değil" bilgisini gösterir.
- **R-4** İzin süresi iş günü olarak hesaplanır: hafta sonu (Cumartesi, Pazar) ve resmi tatiller
  sayılmaz (BR-1). Birim tam gündür (BR-11).
- **R-5** Talep aşağıdaki kuralların tamamını sağlıyorsa kabul edilir; aksi hâlde reddedilir:
  - Başlangıç ≤ bitiş (BR-4).
  - Aralık en az 1 iş günü içerir (BR-4).
  - Başlangıç ≥ bugün; "bugün" Europe/Istanbul takvim günüdür (BR-5, BR-12).
  - Başlangıç ve bitiş aynı takvim yılındadır (BR-13).
  - Çalışanın `Beklemede` veya `Onaylandi` durumundaki hiçbir talebiyle takvim günü olarak
    kesişmez (BR-3). `Reddedildi` / `IptalEdildi` talepler çakışma sayılmaz.
  - `Yillik` için: iş günü sayısı, talep yılının kullanılabilir bakiyesini aşmaz; `Beklemede`
    talepler rezerve olarak düşülmüş sayılır (BR-2). O yıl için izin hakkı tanımlı değilse bakiye
    0 kabul edilir.
  - `Mazeret` ve `Ucretsiz` için bakiye kontrolü yapılmaz; diğer tüm kurallar geçerlidir.
- **R-6** Kabul edilen talep `Beklemede` durumuyla kaydedilir. `Yillik` ise iş günü sayısı kadar
  gün talep yılının bakiyesinden rezerve edilir; diğer türlerde bakiye değişmez.
- **R-7** Talebin oluşturulması, yapan kişi ve UTC zaman damgasıyla denetim izine yazılır.
- **R-8** Reddedilen gönderimde hiçbir talep, rezerve veya denetim kaydı oluşmaz.
- **R-9** Bakiye hiçbir koşulda negatif olmaz; aynı anda yapılan birden fazla gönderim de bu
  değişmezi bozamaz.

## User experience
- **Flow:** Oturum açmış çalışan uygulama menüsünden "İzin Talebi Oluştur" ekranını açar →
  bakiyesini görür → izin türü, başlangıç, bitiş ve (isteğe bağlı) açıklamayı doldurur →
  "Talep Oluştur" ile gönderir → başarı ya da hata durumunu aynı ekranda görür.
- **States:**
  - *Boş (ilk açılış):* alanlar boş, izin türü seçili değil; R-3 bakiye bilgisi görünür.
  - *Hata:* talep alınmadı; ihlal edilen **tüm** kurallar aynı anda, ilgili alanın yanında
    Türkçe mesajla gösterilir; alana bağlanamayan kurallar (çakışma, bakiye) formun üstünde
    gösterilir. Girilen değerler korunur.
  - *Başarı:* "Talebiniz oluşturuldu" mesajı ve özet: izin türü, başlangıç, bitiş, iş günü sayısı,
    durum "Beklemede". Form boşalır; bakiye bilgisi güncel değeri gösterir.
  - *Yetkisiz (oturum yok):* ekran gösterilmez; kullanıcı oturum açma ekranına yönlendirilir.
  - *Yükleniyor:* ayrı bir durum yoktur (sunucu tarafında oluşturulan tek sayfa).
- **Design reference:** none — the flow above is the design (CLARIFY kararı).
- **Accessibility:** her alanın görünür etiketi vardır; form yalnızca klavyeyle (Tab / Enter)
  doldurulup gönderilebilir; hata mesajları ilgili alanla programatik olarak ilişkilendirilir;
  durum bilgisi yalnızca renkle iletilmez.

## Constraints & out of scope
- **Önkoşul:** "Oturum açma ve seed veri" spec'i (ayrı ANALYZE ile açılacak) Shipped olmadan bu
  spec'in BUILD'i başlamaz. O spec; giriş/çıkış, seed çalışanlar ve rolleri (BR-16), seed resmi
  tatilleri ve seed izin haklarını kapsar.
- Tarihler gün bazlıdır; saat bilgisi yoktur. "Bugün" daima Europe/Istanbul'a göredir (BR-12)
  ve kurallar **gönderim anındaki** bugüne göre uygulanır (ekran gece yarısını geçerek açık
  kalsa da).
- Çakışma (BR-3) takvim günü kesişimiyle ölçülür; yalnızca hafta sonunda kesişen aralıklar da
  çakışmadır.
- Bakiye bilgisi yalnızca bugünün yılı için gösterilir; başka yılın bakiye yetersizliği hata
  mesajıyla bildirilir.
- Mesaj metinleri anlam olarak tanımlanır; birebir sabit metinler yalnızca "Bu yıl için izin
  hakkınız tanımlı değil" ve "Talebiniz oluşturuldu"dur.
- Menüye "İzin Talebi Oluştur" girişinin eklenmesi bu spec'e aittir; uygulama iskeleti (layout,
  menü altyapısı) önkoşul spec'e aittir.
- Doğrulama hem ekranda hem modül servisinde yapılır; ekran doğrulaması atlansa da kurallar
  geçerlidir (`docs/security.md`).
- **Out of scope:** onay / ret / iptal; talep listeleri ve talep detay ekranı; talep düzenleme;
  yarım gün izin (BR-11); başka çalışan adına talep; İK'nın hak / tatil tanımlama ekranları;
  canlı (yazarken) iş günü hesaplaması; bildirimler; ileri tarih için BR-13 dışında üst sınır;
  `Mazeret` / `Ucretsiz` için gün limiti.

## Acceptance criteria
Örneklerde "bugün" = 2026-10-21 (Çarşamba, Europe/Istanbul); 2026-10-29 (Perşembe) seed resmi
tatildir; çalışanın 2026 hakkı 14 gün, kesinleşen kullanım 0, rezerve 0'dır (aksi belirtilmedikçe).

**Happy path**
- [ ] AC-1 — `Yillik`, 2026-11-02 (Pzt) – 2026-11-06 (Cum): talep `Beklemede` kaydedilir; başarı
  özeti 5 iş günü ve "Beklemede" gösterir; ekrandaki bakiye 14 → 9 olur.
- [ ] AC-2 — `Mazeret` ve `Ucretsiz` talepleri (geçerli aralık) `Beklemede` kaydedilir; bakiye
  değişmez.
- [ ] AC-3 — Açıklama boş bırakılan geçerli talep kabul edilir; 500 karakterlik açıklama kabul
  edilir.
- [ ] AC-4 — Talep oluşturulunca denetim izine oluşturan kişi ve UTC zaman damgası yazılır.

**İş günü hesabı (BR-1, BR-11)**
- [ ] AC-5 — 2026-10-30 (Cum) – 2026-11-02 (Pzt): 2 iş günü hesaplanır (hafta sonu sayılmaz).
- [ ] AC-6 — 2026-10-28 (Çar) – 2026-10-30 (Cum): 2 iş günü hesaplanır (29 Ekim resmi tatil).

**Sınırlar ve geçersiz girdiler**
- [ ] AC-7 — Başlangıç = bitiş = bir iş günü: 1 iş günü, kabul edilir.
- [ ] AC-8 — Bitiş başlangıçtan önce: reddedilir, bitiş alanında mesaj.
- [ ] AC-9 — Yalnızca hafta sonu / resmi tatil içeren aralık (ör. 2026-10-31 – 2026-11-01 veya
  yalnızca 2026-10-29): 0 iş günü, reddedilir.
- [ ] AC-10 — Başlangıç = bugün (2026-10-21) kabul edilir; başlangıç = dün (2026-10-20)
  reddedilir.
- [ ] AC-11 — "Bugün" Istanbul'a göredir: UTC 2026-10-21 21:30 (= Istanbul 2026-10-22 00:30)
  anında başlangıç 2026-10-21 reddedilir, 2026-10-22 kabul edilir.
- [ ] AC-12 — 2026-12-31 – 2027-01-04 (yıl aşan aralık): reddedilir, "iki ayrı talep açın"
  anlamında mesaj.
- [ ] AC-13 — Zorunlu alanlardan biri veya hepsi boş: gönderim reddedilir, her boş alan için
  ayrı mesaj gösterilir.
- [ ] AC-14 — 501 karakterlik açıklama reddedilir.
- [ ] AC-15 — Geçersiz tarih değeri veya listede olmayan izin türü doğrudan gönderilirse
  reddedilir.

**Çakışma (BR-3)**
- [ ] AC-16 — 2026-11-02 – 11-06 `Beklemede` talep varken 2026-11-05 – 11-10 talebi reddedilir;
  aynısı `Onaylandi` talep için de geçerlidir.
- [ ] AC-17 — Aynı aralık `Reddedildi` veya `IptalEdildi` talepte ise yeni talep kabul edilir.
- [ ] AC-18 — Bitişik aralık (mevcut 11-02 – 11-06, yeni 11-09 – 11-13) kabul edilir.
- [ ] AC-19 — Aynı formun iki kez gönderilmesi ikinci talebi oluşturmaz (çakışma ile reddedilir).

**Bakiye (BR-2, BR-9 rezerve)**
- [ ] AC-20 — Bakiye 14 iken 14 iş günlük `Yillik` talep kabul edilir; 15 iş günlük reddedilir.
- [ ] AC-21 — 2026'da 5 gün rezerve (`Beklemede` talep) varken 10 iş günlük `Yillik` talep
  reddedilir, 9 iş günlük kabul edilir.
- [ ] AC-22 — İzin hakkı tanımlı olmayan yıl (ör. 2027) için `Yillik` talep "Bu yıl için izin
  hakkınız tanımlı değil" mesajıyla reddedilir; aynı yıl için `Mazeret` talep kabul edilir.
- [ ] AC-23 — Kalan bakiyeyi tek başına karşılayabilen iki `Yillik` talep aynı anda
  gönderildiğinde en fazla bakiyenin izin verdiği kadarı kabul edilir; bakiye negatife düşmez.

**Hata davranışı**
- [ ] AC-24 — Birden fazla ihlal içeren gönderimde (ör. geçmiş başlangıç + 501 karakter
  açıklama) tüm mesajlar aynı anda gösterilir ve girilen değerler korunur.
- [ ] AC-25 — Reddedilen her gönderimden sonra talep sayısı, rezerve ve denetim izi değişmemiştir.

**Yetki**
- [ ] AC-26 — Oturumu olmayan kullanıcı ekranı açmak veya form göndermek istediğinde oturum
  açma ekranına yönlendirilir; talep oluşmaz.
- [ ] AC-27 — Gönderime başka bir çalışanın kimliği eklense bile talep yalnızca oturumdaki
  çalışan adına oluşur.
- [ ] AC-28 — Yönetici ve İK rolündeki çalışanlar da kendi adlarına talep oluşturabilir.

**Ekran durumları ve erişilebilirlik**
- [ ] AC-29 — İlk açılışta alanlar boştur, izin türü seçili değildir ve 2026 bakiyesi (14 gün)
  görünür; hak tanımlı değilse R-3 bilgi metni görünür.
- [ ] AC-30 — Başarı sonrası özet (tür, başlangıç, bitiş, iş günü, "Beklemede") görünür ve form
  boştur.
- [ ] AC-31 — Her form alanının görünür etiketi vardır, hata mesajları ilgili alana bağlıdır
  ve form yalnızca klavyeyle doldurulup gönderilebilir.

## Definition of Done
- [ ] Every acceptance criterion mapped to proof (test or reproducible observation)
- [ ] `scripts/check` green
- [ ] Independent review done; real findings fixed, noise rejected with written rationale
- [ ] Docs / ADRs updated if behavior or architecture changed
- [ ] Spec moved to `specs/done/` (it becomes immutable there)

## Scorecard (fill at ship — honest numbers make the process improvable)
| Metric | Value |
|---|---|
| Spec revisions | |
| Fix rounds | |
| Review findings: real / noise | |
| Regressions introduced | |
| Bugs escaped to production | |
