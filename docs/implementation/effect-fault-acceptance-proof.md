# Direct effect/fault acceptance proof

`EffectFaultAcceptanceTests` injects raw `IPage` and `ILocator` implementations with
`DispatchProxy`, then invokes their public wrapped locator actions. It has no
browser dependency and records the actual raw dispatch boundary.

The click and double-click matrix proves that successful trial plus a missing
or unusable bounding box causes no preparatory mouse motion and exactly one
native final action. Fault cases increment an irreversible-effect counter before
returning a genuinely faulted `Task`; assertions require the exact marker
exception instance, its origin stack frame, and no repeated native action.
The separate options check verifies that an explicit native option object is
forwarded by identity and left unchanged.

Run with:

```powershell
dotnet test tests/SpyBrowser.Tests/SpyBrowser.Tests.csproj -c Release --filter FullyQualifiedName~EffectFaultAcceptanceTests
```
