#!/usr/bin/env python3
"""Inspect the produced NuGet artifacts and bind them to their source revision."""
import argparse
import hashlib
import json
import subprocess
import zipfile
from pathlib import Path
import xml.etree.ElementTree as ET

DATA_SHA256 = "1bf3af168719a580e2c5d6fb439f894f9bfff72fc90147149cc531dee80b6203"
REQUIRED = (
    "lib/net8.0/SpyBrowser.Cursory.dll",
    "SpyBrowser.Cursory.nuspec",
    "LICENSE/LICENSE", "LICENSE/COPYING", "LICENSE/COPYING.LESSER",
    "LICENSE/third-party/APACHE-2.0.txt",
    "LICENSE/third-party/NUMPY-BSD-3-CLAUSE.txt",
    "LICENSE/third-party/PCG-MIT.txt", "LICENSE/third-party/PSF-2.txt",
    "NOTICE", "README.md", "provenance/upstream-manifest.json",
    "source/SpyBrowser.Cursory/Data/trajectories.json.gz",
    "source/SpyBrowser.Cursory/Data/upstream-manifest.json",
    "source/SpyBrowser.Cursory/CursoryTrajectoryGenerator.cs",
    "source/SpyBrowser.Cursory/Internal/Dataset.cs",
    "source/BUILD-AND-MODIFY.md",
)


def digest(path: Path) -> str:
    h = hashlib.sha256()
    with path.open("rb") as f:
        for block in iter(lambda: f.read(1024 * 1024), b""):
            h.update(block)
    return h.hexdigest()


def main() -> None:
    ap = argparse.ArgumentParser()
    ap.add_argument("--feed", type=Path, required=True)
    ap.add_argument("--project", type=Path, required=True)
    args = ap.parse_args()
    packages = sorted(args.feed.glob("SpyBrowser.Cursory.*.nupkg"))
    packages = [p for p in packages if not p.name.endswith(".snupkg")]
    if len(packages) != 1:
        raise SystemExit(f"Expected exactly one Cursory nupkg in {args.feed}, found {packages}")
    package = packages[0]
    symbols = package.with_name(package.stem + ".snupkg")
    if not symbols.is_file():
        raise SystemExit(f"Missing portable-symbol package: {symbols}")
    with zipfile.ZipFile(package) as z:
        names = set(z.namelist())
        if "THIRD_PARTY_NOTICES.md" in names:
            raise SystemExit("Package inherited unrelated repository-wide Playwright notices")
        missing = sorted(set(REQUIRED) - names)
        if missing:
            raise SystemExit("Package missing required entries: " + ", ".join(missing))
        dlls = [n for n in names if n.endswith(".dll")]
        if dlls != ["lib/net8.0/SpyBrowser.Cursory.dll"]:
            raise SystemExit(f"Unexpected packaged runtime assemblies: {dlls}")
        if any("/obj/" in f"/{n}" or "/bin/" in f"/{n}" for n in names):
            raise SystemExit("Package accidentally includes obj/bin files")
        data = z.read("source/SpyBrowser.Cursory/Data/trajectories.json.gz")
        if hashlib.sha256(data).hexdigest() != DATA_SHA256:
            raise SystemExit("Packaged source dataset hash mismatch")
        license_hashes = {
            "LICENSE/COPYING": "3972dc9744f6499f0f9b2dbf76696f2ae7ad8af9b23dde66d6af86c9dfb36986",
            "LICENSE/COPYING.LESSER": "7d3a95e5e06978064ed3f8e2b7c8f845e7fd8a405294727cc708f94cb83b8059",
            "LICENSE/third-party/APACHE-2.0.txt": "cfc7749b96f63bd31c3c42b5c471bf756814053e847c10f3eb003417bc523d30",
            "LICENSE/third-party/NUMPY-BSD-3-CLAUSE.txt": "1be1df33863f97a7bc1c4d67980bd6c69c9a6fef0a5ee76e6ad6cb91e56e8491",
            "LICENSE/third-party/PCG-MIT.txt": "290a0ab1748090ac60ee5451da0e2cbf4bfacf532af568c77a3de8d4e6a48440",
            "LICENSE/third-party/PSF-2.txt": "ccedf6b82e8c6e4a163f3352e77b0b1fbc3d90bbf030e63790d22c673211818d",
        }
        for name, expected_hash in license_hashes.items():
            if hashlib.sha256(z.read(name)).hexdigest() != expected_hash:
                raise SystemExit(f"Packaged license text differs from source-verified text: {name}")
        root = ET.fromstring(z.read("SpyBrowser.Cursory.nuspec"))
        namespace = root.tag.partition("}")[0].lstrip("{")
        ns = {"n": namespace}
        metadata = root.find("n:metadata", ns)
        if metadata is None:
            raise SystemExit("Missing nuspec metadata")
        text = lambda tag: (metadata.findtext(f"n:{tag}", default="", namespaces=ns)).strip()
        if text("license") != "LGPL-3.0-or-later":
            raise SystemExit(f"Wrong package license: {text('license')}")
        if "SpyBrowser contributors" not in text("authors") or "Rodrigo Jager" in text("authors"):
            raise SystemExit(f"Incorrect/inherited package authors: {text('authors')}")
        if "Upstream Cursory" not in text("copyright") or "Rodrigo Jager" in text("copyright"):
            raise SystemExit(f"Incorrect/inherited package copyright: {text('copyright')}")
        dependencies = metadata.find("n:dependencies", ns)
        if dependencies is not None and any(group.findall("n:dependency", ns) for group in dependencies):
            raise SystemExit("The package declares a non-BCL runtime dependency")
        dll_hash = hashlib.sha256(z.read("lib/net8.0/SpyBrowser.Cursory.dll")).hexdigest()
    with zipfile.ZipFile(symbols) as zsym:
        pdb_name = "lib/net8.0/SpyBrowser.Cursory.pdb"
        if pdb_name not in zsym.namelist():
            raise SystemExit(f"Missing portable PDB in symbol package: {symbols}")
        pdb = zsym.read(pdb_name)
        if not pdb.startswith(b"BSJB"):
            raise SystemExit("Symbol file is not a portable PDB (missing BSJB metadata signature)")
        pdb_hash = hashlib.sha256(pdb).hexdigest()
    # Prove the included corresponding source/data/project is independently buildable.
    import tempfile
    with tempfile.TemporaryDirectory(prefix="cursory-corresponding-source-") as temp:
        with zipfile.ZipFile(package) as z:
            z.extractall(temp, [n for n in z.namelist() if n.startswith("source/SpyBrowser.Cursory/")])
        source_project = Path(temp) / "source/SpyBrowser.Cursory/SpyBrowser.Cursory.csproj"
        subprocess.run(["dotnet", "build", str(source_project), "-c", "Release"],
                       check=True, capture_output=True, text=True)
    consumer_project = args.project / "tools/Cursory.PackageDemo/Cursory.PackageDemo.csproj"
    consumer_xml = ET.parse(consumer_project).getroot()
    if consumer_xml.findall(".//ProjectReference") or not consumer_xml.findall(".//PackageReference[@Include='SpyBrowser.Cursory']"):
        raise SystemExit("Demo must consume only the NuGet package, never a ProjectReference")
    git = subprocess.run(["git", "-C", str(args.project), "rev-parse", "HEAD"],
                         check=True, capture_output=True, text=True).stdout.strip()
    manifest = {
        "schemaVersion": 1,
        "package": package.name,
        "packageSha256": digest(package),
        "symbolsPackage": symbols.name,
        "symbolsSha256": digest(symbols),
        "portablePdbSha256": pdb_hash,
        "portablePdbFormat": "Portable PDB (BSJB metadata signature)",
        "assemblySha256": dll_hash,
        "sourceCommit": git,
        "datasetSha256": DATA_SHA256,
        "nuspecLicense": "LGPL-3.0-or-later",
        "runtimeDependencies": [],
        "packageContentsVerified": True,
    }
    output = args.feed / "SpyBrowser.Cursory.package-build-manifest.json"
    output.write_text(json.dumps(manifest, indent=2) + "\n", encoding="utf-8")
    print(json.dumps(manifest, indent=2))
    print(f"PASS: package, nuspec, source, data, third-party licenses and symbols verified ({output})")


if __name__ == "__main__":
    main()
