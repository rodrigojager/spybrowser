# Wrapper retention assertion: heap-root analysis

**Workspace baseline:** `d79c085ca4f6e349ad5b0ed744158653ee6382b6`  
**Scope:** `WrapperContractMatrixTests.Frame_collections_payloads_diagnostics_and_weak_lifetimes_follow_contract`

## Finding

The wrapped-context weak-reference failure was a test collection false positive, not a product-owned strong reference. A full testhost dump was captured at the environment-gated assertion pause (`SPYBROWSER_GCROOT_PAUSE`) using the installed `dotnet-dump` executable. The dump is available in the local ignored artifact at `artifacts/wrapper-retention/wrapped-context.dmp`; `root-closed-context.txt` is the corresponding `gcroot` transcript.

The retained target was raw `Microsoft.Playwright.Core.BrowserContext` at `013a95736c60`. The captured root chain was:

```text
WrapperContractMatrixTests.MoveNext (assertion pause)
  -> CreateAndCloseDisposableContextAsync.MoveNext (line 299)
  -> AsyncStateMachineBox<BrowserContext.CloseAsync>d__124 (013a95739830)
  -> BrowserContext (013a95736c60)
```

`dumpvc 7ff8dc4fcae8 013a95739870` showed the close state machine's `<>1__state = -2` and `<>4__this = 013a95736c60`; `dumpobj 013a95736c60` showed `ClosingOrClosed = true`. This was a completed Playwright close state machine visible on the async test/close continuation stack; it was not rooted by `HumanizationScope`, its `ConditionalWeakTable`, `ConfiguredBrowserProxy`, or an event bridge. The control context created through `RawBrowser` was not retained. The other context in the heap was the still-open fixture context, rooted by the test's live generated proxy as expected.

This is why the previous in-thread bounded GC could observe a context retained by the completed Playwright close continuation despite the helper clearing its locals. It was not evidence for a cache leak and did not justify cache eviction or wrapper cleanup.

## Correction and verification

`ForceBoundedCollection` now runs via `Task.Run` and is marked `NoInlining`, collecting from a fresh worker stack after both no-inline create/close helpers have returned. The weak-reference assertions remain unchanged and still check raw and wrapped contexts and pages. The focused real-browser matrix test passed with `SPYBROWSER_RUN_BROWSER_TESTS=1` after this adjustment.

No runtime wrapper/cache/event behavior was changed. In particular, no blanket eviction, proxy disposal, or synthetic event cleanup was added. The temporary environment-gated pause was removed after heap capture.
