# Domain

> Ortak dil: iş birimi, insanlar ve ajanlar aynı kelimeyi aynı anlamda kullanır. Bir terim burada
> yoksa önce buraya eklenir (spec veya ADR ile), sonra kodda kullanılır.

## Ubiquitous language

| Term | Meaning | Notes / not to be confused with |
|---|---|---|
| Çalışan (`Calisan`) | Sisteme giriş yapan ve izin talep eden kişi. Her çalışanın en fazla bir yöneticisi vardır (hiyerarşinin tepesinde yoktur; BR-14). | İK ve yöneticiler de çalışandır. |
| Yönetici (`Yonetici`) | Kendisine bağlı çalışanların taleplerini onaylayan / reddeden çalışan. | Ayrı bir kişi türü değil, bir **rol**. |
| İK (`Ik`) | Tüm talepleri ve bakiyeleri gören, izin hakkı ve resmi tatil tanımlayan rol. | Kendi ekibi yoksa talep onaylamaz; istisna: yöneticisi olmayan çalışanın talebi (BR-14). |
| İzin türü (`IzinTuru`) | MVP'de `Yillik`, `Mazeret`, `Ucretsiz`. | Bakiyeden yalnızca `Yillik` düşer. |
| İzin talebi (`IzinTalebi`) | Bir çalışanın, belirli bir tarih aralığı ve izin türü için yaptığı başvuru. | "İzin" tek başına belirsizdir; talebi mi, hakkı mı kastettiğini belirtin. |
| Talep durumu (`TalepDurumu`) | `Beklemede`, `Onaylandi`, `Reddedildi`, `IptalEdildi`. | `Onaylandi`, `Reddedildi`, `IptalEdildi` son durumlardır. |
| İzin hakkı (`IzinHakki`) | Bir çalışana bir takvim yılı için tanımlanan toplam yıllık izin günü. | Bakiye ile karıştırılmamalı. |
| İzin bakiyesi (`IzinBakiyesi`) | Bir yıl için kullanılabilir yıllık izin günü: hak − kesinleşen kullanım − rezerve. | Hiçbir zaman negatif olamaz. |
| Rezerve (`Rezerve`) | `Beklemede` bir yıllık izin talebinin bakiyeden ayırdığı gün sayısı. | Onayda kesinleşir, ret / iptalde serbest kalır. |
| İş günü (`IsGunu`) | Hafta sonu (Cumartesi, Pazar) veya resmi tatil olmayan gün. | İzin süresi iş günüyle ölçülür, takvim günüyle değil. |
| Resmi tatil (`ResmiTatil`) | İK'nın tanımladığı tatil günleri (MVP'de elle girilen liste). | Arife yarım günleri MVP'de desteklenmez (BR-11); İK o günü ya tam gün tatil olarak girer ya da hiç girmez. |
| Denetim izi (`DenetimKaydi`) | Bir talebin her durum değişikliğinin kim tarafından ve ne zaman yapıldığının kaydı. | Uygulama logu değildir; domain verisidir. |

## Business rules

- **BR-1:** İzin süresi iş günü olarak hesaplanır; hafta sonları ve resmi tatiller sayılmaz.
- **BR-2:** Yıllık izin talebi o yılın kullanılabilir bakiyesini aşamaz; `Beklemede` talepler
  bakiyeden rezerve edilmiş sayılır.
- **BR-3:** Bir çalışanın `Beklemede` veya `Onaylandi` talepleri tarih olarak çakışamaz.
- **BR-4:** Başlangıç tarihi bitiş tarihinden sonra olamaz; talep en az 1 iş günü içerir.
- **BR-5:** Geçmiş tarihli talep oluşturulamaz (başlangıç tarihi ≥ bugün).
- **BR-6:** Talebi çalışanın onaylayıcısı onaylar veya reddeder: kendi yöneticisi; yöneticisi yoksa
  İK (BR-14). Kimse kendi talebini onaylayamaz; İK, yöneticisi olan bir çalışanın talebini onaylayamaz.
- **BR-7:** Reddetmede gerekçe zorunludur.
- **BR-8:** Çalışan yalnızca `Beklemede` talebini iptal edebilir. Onaylanmış iznin iptali MVP
  kapsamı dışıdır.
- **BR-9:** Bakiye yalnızca talep `Onaylandi` durumuna geçtiğinde kesin olarak düşer; ret ve
  iptalde rezerve serbest kalır.
- **BR-10:** İzin hakkını İK tanımlar. Kıdeme göre otomatik hesaplama (İş Kanunu md. 53:
  14 / 20 / 26 gün) MVP kapsamı dışıdır.
- **BR-11:** Birim tam gündür; yarım gün izin yoktur.
- **BR-12:** "Bugün", Europe/Istanbul saat dilimindeki takvim günüdür (sunucunun saat dilimi
  ne olursa olsun). BR-5 ve tüm tarih kontrolleri bu güne göre yapılır.
- **BR-13:** Bir talep iki takvim yılına yayılamaz (başlangıç ve bitiş aynı yıldadır); yıl
  sonunu aşan izin için çalışan iki ayrı talep açar. Böylece her talep tek bir yılın bakiyesini
  etkiler.
- **BR-14:** Yöneticisi olmayan çalışanın (hiyerarşinin tepesi) talebini İK onaylar veya reddeder;
  BR-6 geçerlidir — İK rolündeki kişi de kendi talebini onaylayamaz. Tepedeki kişi İK ise talebini
  başka bir İK onaylar; bu yüzden sistemde en az iki İK bulunur (seed verisi ve İK rolü kaldırma
  işlemi bunu doğrular).
- **BR-15:** İK bir yılın izin hakkını, o yılın kesinleşen kullanım + rezerve toplamının altına
  indiremez (bakiye negatif olamaz değişmezi).

## Key domain invariants

- Son durumdaki (`Onaylandi`, `Reddedildi`, `IptalEdildi`) bir talep değiştirilemez; bu durumlardan
  başka duruma geçiş yoktur. İzinli geçişler yalnızca: `Beklemede` → `Onaylandi` | `Reddedildi` |
  `IptalEdildi`.
- İzin bakiyesi hiçbir zaman negatif olamaz.
- Her durum değişikliği; yapan kişi ve UTC zaman damgasıyla denetim izine yazılır.
