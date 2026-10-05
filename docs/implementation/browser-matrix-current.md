# Browser/platform matrix — source candidate at `35b6656`

**Disposition:** suitable as the documentation cut for a *new local final-source-candidate feed*, not a final-feed, release, or publication approval. Current HEAD is `35b6656caa1b272c074f32b16da67387cb1374df`. `git diff 5fa99de..HEAD -- src` is empty: production `src/` is unchanged since `5fa99decd83295a96b7f2441f1fd3a0f1fca0c3a`. These source-era results therefore describe the same production source; test/harness revisions and package identity still matter. No existing preliminary feed is final-parity evidence.

## Evidence matrix

| Platform/lane | Playwright/browser | Playwright tests | Cursory tests | Evidence / scope |
|---|---|---:|---:|---|
| Linux required | 1.61.0, Ubuntu 24 / Xvfb, non-root | 177 passed, 0 skipped | 37 passed, 0 skipped | `artifacts/goal/linux-5fa99de-zero-skip/required161/`; exact source archive 5fa99de |
| Linux reviewed | 1.63.0, Ubuntu 24 / Xvfb | 177 passed, 0 skipped | 37 passed, 0 skipped | `artifacts/goal/linux-5fa99de-zero-skip/reviewed163/`; reviewed lane, not a minimum-version promotion |
| Linux Chrome | 1.61.0, Chrome 154.0.8037.97 / Xvfb | 177 passed, 0 skipped | 37 passed, 0 skipped | `artifacts/goal/linux-5fa99de-zero-skip/chrome161/` |
| Windows required | 1.61.0 | 177 passed | 37 passed | `artifacts/goal/5fa99de-required-results/`, `5fa99de-cursory-results/` |
| Windows reviewed | 1.63.0 | 177 passed | 37 passed | `artifacts/goal/5fa99de-latest-results/` |
| Windows Chrome | stable Chrome | 177 passed | Cursory separately 37 passed | `artifacts/goal/5fa99de-chrome-results/` |
| Windows Edge | Edge, source-compatible test cut `d574f6b` | 177 passed | — | `artifacts/goal/d574f6b-edge-results/`; production source unchanged from 5fa99de |

Linux zero-skip run-status and six TRX counters establish 531/531 Playwright and 111/111 Cursory passed across the three lanes. The lane flags enable browser/headed behavior, but do **not** force every fixture to use channel Chrome or headed mode. The Windows Edge result is the separate `d574f6b` fixture/test cut; describe it as source-compatible, not as a run at current HEAD. Older `776362f` and `b1777b3` matrices are historical and are not being promoted as this cut's acceptance.

## Consumer and package evidence (preliminary, source-bound)

- Actual RpaBlockly adapter/event harness consumed `C:/Temp/spybrowser-feed-review-5fa99de` (version `0.2.0-beta.2.review.5fa99de`): **14/14** named required checks for Cursory + PlaywrightCompatible + Humanize on. Its separate semantic map records **17** individually asserted operations; 14 is not a claim of 17 or exhaustive API coverage. See [recovery-consumer-event-proof](recovery-consumer-event-proof.md). Historical Bézier/Legacy-on and Humanize-off runs each passed **13** checks on that older harness; they are not the new 14-check event proof.
- Linux installed-package run: 13 technical checks passed, `technicalAllPassed=true`, while rights remain BLOCKED, `allPassed=false`, and `externalPublicationAllowed=false`. Windows installed evidence is under `artifacts/goal/installed-5fa99de-windows-proof/`. Both are preliminary feed evidence, not final-feed parity or signoff.
- Preliminary package inspection checked package contents, license/source/PDB material and source rebuild. A separate-DLL arrangement is not legal clearance.

## Reproduction and interpretation

Use exact commands, SDK/environment, TRX, and source-archive hash in [Linux zero-skip proof](linux-zero-skip-proof.md) and [recovered Linux proof](recovered-linux-proof.md). For Windows, use logs/results under the evidence paths above. Test results are source-era results; they do not attest to an as-yet-unbuilt package tied to current HEAD. Required Playwright remains 1.61.0; 1.63.0 is a reviewed lane only. The original earlier Linux skipped-probe runs remain historical records and were not relabeled; the zero-skip rerun explicitly enabled the probe.

## Gate before calling the installed candidate final

After the documentation/source cut is frozen, build a **new** local feed from the complete source candidate, bind its full SHA and package/source hashes, inspect package contents, and rerun Windows/Linux installed-package and consumer checks against those exact packages. Then reconcile the remaining ticket/ledger criteria individually. Until those steps, do not claim a final feed, package parity, all 260 criteria complete, publication permission, rights clearance, or default promotion. External publication remains blocked by dataset redistribution rights; no current-final-feed parity is established.
