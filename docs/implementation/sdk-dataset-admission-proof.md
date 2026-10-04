# SDK dataset admission proof

Selecting the experimental Cursory mouse algorithm now validates the embedded trajectory dataset at the SDK action boundary, before locator waits/trials/scrolling or native mouse dispatch. Validation is read-only and uses the generator's existing `Dataset.Shared` lazy integrity checks; it does not synthesize a path, advance its random stream, or eagerly load data when `HumanActions` is constructed. Other mouse algorithms do not access the dataset.

`SDKDatasetAdmissionFaultTests` mutates the actual Cursory PE resource and loads that assembly together with a separately loaded SDK assembly in a collectible load context, while sharing Playwright interfaces and dependencies from the default context. The unknown-pointer Cursory move is asserted to fail with `InvalidDataException` and zero mouse moves for both damaged and missing-resource images. Healthy data preserves the one native anchor dispatch and allows a later generated movement; Bezier remains independent of dataset validity.

Validation target: `dotnet test tests/SpyBrowser.Tests/SpyBrowser.Tests.csproj --filter "FullyQualifiedName~SDKDatasetAdmissionFaultTests"`. Native Windows execution is recorded with the implementation handoff; Linux verification is pending if the WSL runtime remains unavailable.
