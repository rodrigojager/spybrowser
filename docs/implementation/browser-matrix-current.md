# Browser/platform matrix — ef2d0ea local candidate

Package identity is `ef2d0eaa35a5bf1950acf75a342590b95e15c9e0`, version `0.2.0-beta.2.final.ef2d0ea`. Production C# remains unchanged from `5fa99decd83295a96b7f2441f1fd3a0f1fca0c3a`; later Cursory project changes concern toolchain/source packaging. SDK-8 source/program correspondence is separately proven, not whole-PE byte identity. Full browser suites below were run from frozen **4d8a18c** source; do not relabel them as ef2d executions. Final installed and consumer checks below use actual **ef2d** package bytes.

## Source browser contracts

| Platform/lane | Playwright | Product | Cursory | Direct evidence |
|---|---|---:|---:|---|
| Windows required | 1.61.0 | 177 passed, zero skips | 37 passed, zero skips | `windows-final-4d8a18c/required161/` |
| Windows reviewed | 1.63.0 | Initial 176 passed / 1 failed; focused diagnostic 1 passed; sequential full 177 passed / zero skips | 37 passed | `windows-final-4d8a18c/reviewed163/` |
| Windows Chrome | 1.61.0 | 177 passed, zero skips | Separately above | `windows-final-4d8a18c/chrome177/` |
| Windows Edge | 1.61.0 | 177 passed, zero skips | Separately above | `windows-final-4d8a18c/edge177/` |
| Linux required | 1.61.0 | 177 passed, zero skips | 37 passed, zero skips | `linux-final-4d8a18c/required161/` |
| Linux reviewed | 1.63.0 | 177 passed, zero skips | 37 passed, zero skips | `linux-final-4d8a18c/reviewed163/` |
| Linux Chrome | 1.61.0 | 177 passed, zero skips | 37 passed, zero skips | `linux-final-4d8a18c/chrome161/` |

Paths above are relative to `artifacts/goal/`. Exact commands, SDK 8.0.319/runtime 8.0.22, archive identities, browsers and TRXs are in [Windows proof](windows-final-source-proof.md) and [Linux proof](linux-final-source-proof.md). Linux used Ubuntu 24.04, non-root UID 1001 and Xvfb. Windows Chrome was 154.0.8037.93, Edge 154.0.4258.53; Linux Chrome 154.0.8037.97. Lane flags do **not** prove every fixture launched that browser channel or headed mode. Xvfb is not hardware-GPU evidence.

The retained Windows reviewed failure is `Probe_preserves_user_pages_and_concurrent_new_pages_in_all_launch_modes`, at the existing five-second late-page `NewPageAsync` completion bound. The exact test then passed focused and in the sequential complete suite without changed assertions/timeouts. A stronger OS/browser cause is not established; original failure remains visible. Linux's six TRXs contain 531 product and 111 Cursory passes, with zero skips/errors/aborts.

Required Playwright remains 1.61.0; reviewed 1.63.0 is not a minimum-version/default promotion. Older source/preliminary feeds remain historical evidence.

## Actual final-package verification

- [Windows installed](ef2d0ea-installed-windows-proof.md) and [Linux installed](ef2d0ea-installed-linux-proof.md): each 13 technical checks passed; no technical failures/pending. Linux actual SDK8.0.319, UID1001, cached Chrome for Testing153.0.8010.12. Both runners retain rights-only `BLOCKED`, `allPassed=false`, external publication false.
- [RpaBlockly](ef2d0ea-consumer-proof.md): each of three final-feed lanes 14/14 and final parity true; separate 17-group semantic assertion map. Actual isolated CWD SDK10.0.401, net9 consumer, Playwright1.61.0/bundled Chromium1228. Pinned original checkout untouched. Mixed provider/CAPTCHA/network suites not run.
- [Package inspection](ef2d0ea-package-proof.md): all four package/symbol pairs and provenance verified, 4 actual raw SourceLink CDIs /58 tracked checksums, 432/432 corresponding program entries. [Display-field annotation](ef2d0ea-source-map-report-note.md) preserves the immutable producer manifest and actual CDI distinction.
- [Final native/benchmark/historical proof](ef2d0ea-native-proof.md): package-only BCL assertions,37/37 Cursory,28 completed historical page observations,37 old API types with no removals/default changes and74 additive members. Three-process median cold401.380ms/p507.563ms/p9511.049ms/2,160,426allocated bytes. Timing observations are not universal CI guarantees; old baseline RNG cannot support paired trajectory comparisons. Raw outputs remain in `cursory-ef2d0ea-results/`, `native-package-demo-ef2d0ea/` and `historical-ef2d0ea/`.
- A later **test-only** closure for criterion06.09 directly starts a pending compatible Fill and closes its context, then awaits the original task:1/1 focused Chromium pass, first assertion failure preserved. [Proof](final-fill-context-close-proof.md). This is not a full-suite rerun or package-source relabel.

External dataset rights remain UNVERIFIED and publication/default promotion prohibited. Local technical results are not legal clearance or external-release approval.
