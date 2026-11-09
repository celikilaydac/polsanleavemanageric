# Spec 0004 — Taleplerim ve Geçmiş sekmeleri

- Status: Shipped
- Mode: lite
- Plan: `specs/plans/0004-plan.md`

## Intent
Çalışan bugün izin talebi oluşturabiliyor (spec 0001) ama verdiği talepleri ve onların başına
gelenleri göremiyor. Bu iş, oturum açmış her çalışana iki salt-okunur sekme ekler: **Taleplerim**
(kendi izin taleplerinin listesi ve durumları) ve **Geçmiş** (kendi taleplerinin denetim izi: hangi
talep, ne zaman, hangi durumdan hangisine, kim tarafından). Başarı: çalışan, oluşturduğu her talebi
ve her durum değişikliğini doğru bilgilerle, yalnızca kendisininkileri görür. Bilinçli olarak
yapılmayanlar: talep üzerinde işlem (iptal, düzenleme), filtreleme / sayfalama / arama, talep detay
ekranı, yönetici veya İK'nın başkalarının taleplerini gördüğü ekranlar.

## Requirements
- **R-1** Oturum açmış her çalışan (rolü ne olursa olsun) menüde "Taleplerim" ve "Geçmiş"
  sekmelerini görür ve açabilir. Her iki sekme yalnızca oturumdaki çalışanın kendi verisini
  gösterir; başka bir çalışanın verisini görmenin hiçbir yolu yoktur (adres parametresiyle de).
- **R-2** Taleplerim, çalışanın tüm izin taleplerini (her durumda) listeler; her satırda izin türü
  (Türkçe ad), başlangıç, bitiş, iş günü sayısı, durum (Türkçe ad) ve açıklama bulunur. Sıra:
  başlangıç tarihi yeniden eskiye; aynı başlangıçta bitiş tarihi yeniden eskiye.
- **R-3** Geçmiş, çalışanın taleplerine ait tüm denetim kayıtlarını listeler; her satırda zaman
  (Europe/Istanbul, gün ve dakika), ilgili talep (tür ve tarih aralığı), değişiklik ve yapan kişi
  bulunur. Sıra: zaman yeniden eskiye.
  - Değişiklik: önceki durum yoksa "Oluşturuldu → <yeni durum>", varsa "<eski> → <yeni>".
  - Yapan: kayıt çalışanın kendisine aitse "Siz"; değilse yapan çalışanın adı soyadı.
- **R-4** Türkçe adlar: türler "Yıllık", "Mazeret", "Ücretsiz"; durumlar "Beklemede",
  "Onaylandı", "Reddedildi", "İptal edildi". Tarihler gg.aa.yyyy; zaman gg.aa.yyyy SS:dd.
- **R-5** Sekmeler salt-okunurdur; hiçbir kayıt oluşturmaz veya değiştirmez.

## User experience
- **Flow:** Oturum açmış çalışan menüden "Taleplerim" veya "Geçmiş"i seçer → listeyi görür.
  "İzin Talebi Oluştur" ekranında talep oluşturduktan sonra yeni talep Taleplerim'de, oluşturulma
  kaydı Geçmiş'te görünür.
- **States:**
  - *Dolu:* tablo; durum metinle yazılır (yalnızca renkle değil).
  - *Boş:* Taleplerim'de "Henüz izin talebiniz yok."; Geçmiş'te "Henüz kayıt yok." Tablo gösterilmez.
  - *Yetkisiz (oturum yok):* sayfa gösterilmez; giriş ekranına yönlendirilir.
  - *Yükleniyor:* ayrı bir durum yoktur (sunucu tarafında oluşturulan sayfa).
  - *Hata:* beklenmeyen hata genel hata sayfasıyla (spec 0002).
- **Design reference:** none — the flow above is the design.
- **Accessibility:** tabloların görünür başlığı (`caption`) ve sütun başlıkları vardır; menüde
  açık sekme programatik olarak işaretlenir; sayfalar yalnızca klavyeyle gezilebilir.

## Constraints & out of scope
- Açıklama ve adlar kullanıcıya düz metin olarak gösterilir (HTML olarak yorumlanmaz).
- Hedef ölçekte (50–500 kullanıcı) sayfalama gerekmez.
- **Out of scope:** iptal / düzenleme / detay; filtre, arama, sayfalama, dışa aktarma; yönetici ve
  İK ekranları; bakiye hareketleri geçmişi.

## Acceptance criteria
Örneklerde "bugün" = 2026-10-21 (Istanbul); ik1 = Elif Kaya.

**Menü ve yetki**
- [x] AC-1 — Oturum açık kullanıcının menüsünde "Taleplerim" ve "Geçmiş" vardır, var olan
  sayfalara gider; açık sekme menüde işaretlidir.
- [x] AC-2 — Oturumu olmayan kullanıcı iki sekmeden birini açmak istediğinde giriş ekranına
  yönlendirilir.

**Taleplerim**
- [x] AC-3 — Üç talep (Yıllık 11-02–11-06, Mazeret 11-16, Ücretsiz 12-01–12-02) oluşturulmuşken
  üçü de doğru tür, tarih, iş günü (5, 1, 2), "Beklemede" ve açıklamayla, başlangıca göre
  yeniden eskiye listelenir.
- [x] AC-4 — Onaylandi, Reddedildi ve IptalEdildi durumundaki talepler "Onaylandı", "Reddedildi",
  "İptal edildi" olarak gösterilir.
- [x] AC-5 — Açıklaması olmayan talepte açıklama "—" gösterilir; `<b>` gibi işaretleme içeren
  açıklama düz metin olarak görünür.
- [x] AC-6 — Başka bir çalışanın talepleri listede görünmez; adrese başka çalışanın kimliği
  eklense de liste değişmez.
- [x] AC-7 — Hiç talebi olmayan çalışan "Henüz izin talebiniz yok." görür; tablo yoktur.

**Geçmiş**
- [x] AC-8 — Talep oluşturulunca Geçmiş'te bir satır görünür: zaman Istanbul saatiyle
  (UTC 09:00 → "21.10.2026 12:00"), talep "Yıllık 02.11.2026 – 06.11.2026", değişiklik
  "Oluşturuldu → Beklemede", yapan "Siz".
- [x] AC-9 — Zaman Istanbul gününe göredir: UTC 2026-10-21 21:30 kaydı "22.10.2026 00:30" görünür.
- [x] AC-10 — Birden fazla kayıt zamana göre yeniden eskiye sıralıdır.
- [x] AC-11 — Başkası tarafından yapılan durum değişikliği (ör. yöneticinin Beklemede → Onaylandi)
  "Beklemede → Onaylandı" ve yapanın adı soyadıyla görünür.
- [x] AC-12 — Başka çalışanın taleplerine ait kayıtlar görünmez.
- [x] AC-13 — Hiç kaydı olmayan çalışan "Henüz kayıt yok." görür; tablo yoktur.

**Erişilebilirlik ve salt-okunurluk**
- [x] AC-14 — İki tabloda da görünür `caption` ve `scope="col"` sütun başlıkları vardır; durum
  metinle yazılır; sayfa içeriğinde (`main`) form yoktur ve sayfalarda script yoktur (menüdeki
  "Çıkış" formu spec 0002'ye aittir).
- [x] AC-15 — Sekmeleri açmak talep, denetim veya bakiye kaydı sayısını değiştirmez.

## Definition of Done
- [x] Every acceptance criterion mapped to proof (test or reproducible observation)
- [x] `scripts/check` green
- [x] Independent review done; real findings fixed, noise rejected with written rationale
- [x] Docs / ADRs updated if behavior or architecture changed
- [x] Spec moved to `specs/done/` (it becomes immutable there)

## Scorecard (fill at ship — honest numbers make the process improvable)
| Metric | Value |
|---|---|
| Spec revisions | 1 (AC-14 netleştirildi: form kontrolü `main` ile sınırlı; review P-2) |
| Fix rounds | 3 (review P, re-review Q, VERIFY S); fix round içinde 1 kararsız test yakalandı ve düzeltildi |
| Review findings: real / noise | 8 real / 1 noise (P-1…P-6, Q-1, S-1 real; Q-2 noise). VERIFY: 15/15 kanıtlı; 15 mutasyonun 14'ü yakalandı, kalan S-1 ile kapatıldı |
| Regressions introduced | 0 |
| Bugs escaped to production | 0 (henüz dağıtım yok) |
