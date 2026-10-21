# Git

> Güvenli varsayılanlar korunmuştur; gevşetme bilinçli yapılır, kazara değil.

## Branching
- `feature/<spec-no>-<short-name>` — **no branch without a spec.** Also for CHANGE-lane mini-specs.
- Refactors: `refactor/<spec-no>-<short-name>` (mini-spec with Changed behavior = none).
- Fixes: `fix/<report-id>-<short-name>` (bug fixes carry a report, not a spec number);
  incidents: `incident/<date>-<short-name>`; trivial changes: `trivial/<short-name>` (see below).
- İş alanı / araç değişiklikleri: `chore/<short-name>` — yalnızca ANEW çekirdeği ve adapter'ları,
  CI, editör/araç ayarları ve **mevcut** paketlerin sürüm yükseltmesi. Spec gerekmez; PR iş kalemini
  adlandırır, bağımsız review + `scripts/check` yeşil zorunludur.
  - Chore **değildir** (spec veya ADR gerekir): `docs/domain.md`, `docs/architecture.md`,
    `docs/security.md` kuralları (BR, YB, güvenlik), yeni paket eklemek (`docs/security.md`).
  - Şu yollara dokunan bir chore yeniden triage edilir (`workflows/change-request.md`): `src/`,
    `tests/`, `scripts/check.conf`, `Directory.*.props`, `global.json`, `.editorconfig`.
- Önekler İngilizce kalır; `<short-name>` Türkçe, ASCII, kebab-case'dir:
  `feature/0001-yillik-izin-talebi`, `trivial/buton-metni`, `incident/2026-10-20-giris-hatasi`.
- Default branch: `main` (`origin` = `github.com/celikilaydac/polsanleavemanageric`).

## Commits
- Conventional Commits, with a plan reference: `feat(izinler): yıllık izin talebi oluşturma [plan 0001/2]`.
- Tür İngilizce (`feat`, `fix`, `refactor`, `test`, `docs`, `chore`, `ci`); kapsam modül adı,
  küçük harf (`izinler`, `bakiyeler`, `takvim`, `calisanlar`, `web`, `ortak`, `mimari`); açıklama Türkçe,
  Türkçe karakter serbest.
- Agent commits follow the same standard: the agent writes the message, the human approves.

## Forbidden
- Direct commits to the default branch.
- Force push, history rewriting on shared branches. Undo = `git revert` (see recovery R-11).

## Trivial changes
<!-- Set at bootstrap. Applies only to requests the change-request triage rubric
     (workflows/change-request.md) classifies as TRIVIAL: no acceptance criterion changes, no shared code. -->
- Policy: **(1) PR + one reviewer, no spec** — `trivial/<short-name>` branch'i, PR açılır, CI yeşil.
  Tek geliştiricili bu projede reviewer = bağımsız review (Claude Code `reviewer` alt ajanı veya
  ayrı oturum); PR'ı merge eden insandır.
- Always: one narrow commit, `scripts/check` green. A second file or a new test means re-triage.

## Pull requests
- PR template checklist completed; `scripts/check` green in CI; squash-merge.
- GitHub ayarı (repo dışında, insan yapar): `main` için branch protection — `check` ve `doctor`
  CI işleri zorunlu, force push kapalı. Tek geliştirici olduğu için "PR review zorunlu" kapalıdır.
