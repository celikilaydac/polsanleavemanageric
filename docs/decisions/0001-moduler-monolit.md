# ADR 0001 — Modüler monolit mimarisi

- Status: Accepted
- Date: 2026-10-19

## Context
PolsanIzin, 50–500 kullanıcılı bir şirket içi izin yönetim sistemidir ve bir eğitim projesidir.
Tek geliştirici çalışır; operasyonel altyapı (Docker, mesaj kuyruğu, birden çok servis) yoktur.
Domain küçük ama birbirinden ayrışan sorumluluklar içerir: çalışan / rol, takvim, bakiye, talep
yaşam döngüsü. Eğitim hedefi, modül sınırlarının nasıl korunduğunu öğretmektir.

## Decision
Tek dağıtılabilir uygulama ve tek SQLite veritabanından oluşan bir **modüler monolit** kurulur;
modüller (`Calisanlar`, `Takvim`, `Bakiyeler`, `Izinler`, `Web`) ayrı class library projeleridir,
yalnızca public arayüzlerle senkron konuşur, kendi DbContext ve tablolarına sahiptir ve sınırlar
derleyici (`internal`) ile mimari testler (YB-1…YB-6, `docs/architecture.md`) tarafından korunur.
Modüllerin paylaştığı altyapı tipleri (`Sonuc`, `Hata`, `Bugun()`) dar bir paylaşılan çekirdekte,
`PolsanIzin.Ortak`'ta durur: tüm projeler ona referans verebilir, o hiçbirine veremez ve domain
tipi içermez (YB-6). Aksi halde YB-3 altında ortak sonuç deseni paylaşılamazdı.

## Consequences
- Kazanç: tek build, tek dağıtım, tek test komutu; modül sınırları test edilebilir; ileride bir
  modülü ayırmak gerekirse sınır zaten çizilidir.
- Maliyet: modüller arası çağrılar arayüz katmanı gerektirir (biraz tören); tek veritabanı
  dosyası, şema sahipliğinin disiplinle (ayrı DbContext) korunmasını gerektirir; modüller arası
  işlem bütünlüğü (ör. talep + bakiye rezervasyonu) dikkatle tasarlanmalıdır.

## Alternatives considered
- **Mikroservisler:** bu ölçek ve tek geliştirici için ağ, dağıtım ve tutarlılık maliyeti
  kazanımı aşar.
- **Katmanlı tek proje (Controller / Service / Repository):** daha az yapı, ama modüller arası
  erişim derleyici veya test tarafından engellenemez; sınır ihlalleri sessizce birikir.

## Revisit triggers
- Bir modülün bağımsız ölçeklenmesi veya ayrı ekip tarafından dağıtılması gerekirse.
- Modüller arası senkron çağrılar döngüye zorlamaya başlarsa (olay tabanlı iletişim ADR'si).
- Kullanıcı sayısı veya veri hacmi SQLite sınırlarına yaklaşırsa (ayrı ADR: veritabanı).
