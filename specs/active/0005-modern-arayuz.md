# Spec 0005 — Modern arayüz ve koyu mavi–mor–pembe palet (mini)

- Status: Approved
- Mode: lite
- Plan: `specs/plans/0005-plan.md`
- Source: kullanıcı talebi (sohbet, 2026-11-09): "ux / ui yapar mısın, modern kullanıcı arayüzü
  olsun"; örnek palet: magnific.com "mavi-mor-pembe renk paleti, koyu renkler" (sayfa otomatik
  okunamadı — HTTP 403; palet başlıktaki tarife göre kuruldu, kesin değerler gelirse yalnızca
  renk değişkenleri değişir)
- Design: none — palet ve kurallar aşağıda; "önce" görünümü spec 0002 / 0001 / 0004 PR ekleri

## Intent
Uygulama bugün işlevsel ama sade, açık renkli ve tasarımsız. Kullanıcı modern bir arayüz istiyor:
koyu mavi–mor–pembe tonlarda, kart düzenli, sekmeli menüsü olan, okunabilirliği ve erişilebilirliği
koruyan bir görünüm. Davranış (akışlar, kurallar, metinler) değişmez; yalnızca görünüm değişir.

## Changed behavior
- [ ] CB-1 — Tüm sayfalar koyu mavi–mor–pembe paleti kullanır; renkler yalnızca stil dosyasındaki
  değişkenlerde tanımlıdır (başka yerde sabit renk yoktur).
- [ ] CB-2 — Palet WCAG AA kontrastını sağlar: metin / zemin çiftleri ≥ 4.5:1; odak göstergesi ve
  form kenarı / zemin çiftleri ≥ 3:1 (testle hesaplanır).
- [ ] CB-3 — Üst bar uygulama adını ve sekme (hap) görünümlü menüyü gösterir; açık sekme yalnızca
  renkle değil yazı kalınlığı ve alt çizgiyle de ayırt edilir.
- [ ] CB-4 — Talep durumu (Taleplerim) rozet olarak gösterilir; rozetin metni durumun Türkçe adıdır
  (renk yalnızca ek ipucudur).
- [ ] CB-5 — İçerik kartlarda gösterilir; giriş ekranı ortalanmış tek kart; dar ekranda (≤ 640px)
  form ve menü tek sütuna iner, sayfa yatay kaymaz (tablolar kendi kapsayıcısında kayar).

## Preserved behavior
- [ ] PB-1 — Spec 0002, 0001 ve 0004'ün kabul kriterlerini kanıtlayan testlerin tamamı değişmeden
  yeşil kalır (metinler, kimlikler, akışlar aynı).
- [ ] PB-2 — Her akış yalnızca klavyeyle yapılabilir; odak her etkileşimli öğede görünür.
- [ ] PB-3 — Sayfalarda script yoktur; dış kaynak (font, CDN, görsel) yüklenmez; güvenlik
  başlıkları aynıdır.
- [ ] PB-4 — Durum ve hata bilgisi yalnızca renkle iletilmez (metin her zaman vardır).

## Out of scope
Açık / koyu tema seçici; ikon seti, logo, görsel varlıklar; animasyon kütüphaneleri; yeni sayfa,
yeni bağlantı veya metin değişikliği; JavaScript.
