# Spec 0003 — Giriş sınırlayıcısı testleri ve ölçümü (mini)

- Status: Draft
- Mode: lite
- Plan: `specs/plans/0003-plan.md`
- Source: review 0002 takip işleri H-3, H-4, J-1 (`specs/plans/0002-plan.md`, Amendments: "VERIFY fix round review")

## Intent
Spec 0002'de yerleşik rate limiter yerine kendi `GirisSinirlayici`'mız geldi. AC-9 tek bölümle
(TestServer'da IP `null`) kanıtlı; IP başına ayrım, `null` IP anahtarı, temizlik yolu ve tekdüze
saat koruması testle korunmuyor, kilit altındaki temizlik taramasının maliyeti de ölçülmedi. Bu iş,
davranışı değiştirmeden bu boşlukları testle kilitler ve maliyeti ölçer.

## Changed behavior
none — no observable behavior change

## Preserved behavior
- [ ] PB-1 — Bir IP pencere sınırına ulaştığında başka bir IP'nin denemeleri etkilenmez (H-3).
- [ ] PB-2 — IP'si `null` olan istekler ile `"bilinmeyen"` anahtarı aynı sayacı paylaşır (H-3).
- [ ] PB-3 — 1024'ten fazla IP varken yapılan temizlik, süresi dolmamış bir IP'nin sayacını
  silmez (H-3).
- [ ] PB-4 — Duvar saati geri alınsa da (`GetUtcNow` geri, `GetTimestamp` ileri) pencere kaymaz
  (J-1).
- [ ] PB-5 — 10.000 farklı IP ile bir pencere içindeki toplam süre ölçülür ve kayda geçer; kabul
  edilemezse (eşik plan aşamasında belirlenir) temizlik zamana bağlanır (H-4, R-12: önce ölçüm).
- [ ] PB-6 — Spec 0002 AC-9 testleri (`GirisSiniriTests`) değişmeden yeşil kalır.

## Out of scope
Sınırlama politikasının (10 deneme / 1 dk) değiştirilmesi; dağıtık / çok sunuculu sayaç; hesap
kilitleme; `ForwardedHeaders` yapılandırması.
