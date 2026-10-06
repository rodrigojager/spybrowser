# ef2d0ea final-feed RpaBlockly consumer proof

**Result:** all three real-consumer lanes passed, each with **14/14 required named checks**, `candidateFinalParity: true`, and exit code 0. This is evidence against the exact final package feed—not a locally rebuilt package or the producer staging directory.

## Provenance and environment

- Source/final commit: `ef2d0eaa35a5bf1950acf75a342590b95e15c9e0`; version: `0.2.0-beta.2.final.ef2d0ea`.
- Feed consumed: `D:/SpyBrowser/artifacts/goal/final-feed-ef2d0ea/`; producer feed was inspected at `D:/Temp/spybrowser-feed-final-ef2d0ea/` only for independent producer-manifest provenance.
- `consumer-ready.json` reports `status=ready`, `isFinal=true`; final manifest, producer manifest, and on-disk nupkg SHA-256 values agree. All three nuspec versions and repository commits match the full final commit. PDB/SourceLink audit is ready and maps source URLs to the full commit.
- Exactly three candidate packages were used: Core `7c3f64b2fb65cc4c706ce2f4d2b39f553e3f548e210384d4692e51fd6fd1e194`, Cursory `b042e0c8643b00b375bbf20d1d65ee458b990472592f064bb5a741e581ce3f10`, and Playwright `17965e54e3828a8d11c8686afafaf768653629ee2f68ea5dbd3e03de9a28363b`. Hashes are full SHA-256 and identical in all lanes.
- Consumer checkout `D:/RpaBlockly` remained pinned at `c2f2947c3ccd8b20f7a1cdf9c3b41fb68567b6ca`; verification used isolated `git archive` copies. Target is net9.0. SDK requested by the consumer is 10.0.302 with latestFeature roll-forward; **10.0.401 was resolved and logged inside each actual isolated consumer cwd**. Root SDK 8.0.319 was not used as consumer-SDK evidence.
- Playwright `1.61.0`; Chromium launched via the actual pinned `BrowserLauncher`, revision `1228`, full browser version `149.0.7827.55` (package `browsers.json`, installed chromium-1228). Local loopback only; no provider/network flow was claimed.
- Manifest states dataset rights `UNVERIFIED` and `externalPublicationAllowed=false`.

## Sequential lanes

| Lane | Configuration | Result | Evidence |
|---|---|---|---|
| `cursory-compatible-on` | cursory / playwright-compatible / on | 14/14, final parity true, exit 0; SDK 10.0.401 | [evidence](../../artifacts/goal/consumer-ef2d0ea-cursory-compatible-on/evidence.json) · [harness](../../artifacts/goal/consumer-ef2d0ea-cursory-compatible-on/harness-evidence.json) · [log](../../artifacts/goal/consumer-ef2d0ea-cursory-compatible-on/run.log) |
| `bezier-legacy-on` | bezier / legacy / on | 14/14, final parity true, exit 0; SDK 10.0.401 | [evidence](../../artifacts/goal/consumer-ef2d0ea-bezier-legacy-on/evidence.json) · [harness](../../artifacts/goal/consumer-ef2d0ea-bezier-legacy-on/harness-evidence.json) · [log](../../artifacts/goal/consumer-ef2d0ea-bezier-legacy-on/run.log) |
| `cursory-compatible-off` | cursory / playwright-compatible / off | 14/14, final parity true, exit 0; SDK 10.0.401 | [evidence](../../artifacts/goal/consumer-ef2d0ea-cursory-compatible-off/evidence.json) · [harness](../../artifacts/goal/consumer-ef2d0ea-cursory-compatible-off/harness-evidence.json) · [log](../../artifacts/goal/consumer-ef2d0ea-cursory-compatible-off/run.log) |

Each lane used its own output, `TMP`/`TEMP`, `DOTNET_CLI_HOME`, and `NUGET_PACKAGES`; execution was sequential and each lane had a 1800-second process-tree timeout. The full mixed CAPTCHA/network/provider suite is explicitly **not run**.

## Fourteen aggregate checks vs. seventeen semantic operations

The harness reports **14 aggregate required check names** per lane, compared by exact name; it does not report “17 checks.” To retain the finer-grained contract without conflating those counts, the companion [JSON](ef2d0ea-consumer-proof.json) separately maps **17 semantic assertion operations** to exact frozen `Program.cs` assertions and line references. These assertion groups were exercised within the successful harness runs (a failed `Require` fails the lane). Highlights include exact-once context/page events and unsubscribe/resubscribe identity, popup/nested-frame wrapper behavior, parsed V1/V2 compiler-to-real-RpaRunner flows, URL wait armed before the click, storage/cookie restoration, cancellation, concurrent job/RNG isolation, and delayed-browser cleanup.

The proof JSON records the exact 14 aggregate names, all 17 source assertion mappings, artifact/package hashes, lane paths, source hashes, provenance, and scope limitations. Consumer-side temporary modifications were limited to the frozen harness's package/configuration injection and test-only project; there was no candidate project reference, repack, fixture/effect/deadline relaxation, source/API/default change, or edit to the original consumer checkout.
