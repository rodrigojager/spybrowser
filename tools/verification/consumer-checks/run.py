#!/usr/bin/env python3
"""Build or consume a local SpyBrowser feed and run the pinned RpaBlockly subset."""
from __future__ import annotations
import argparse, datetime as dt, hashlib, json, os, pathlib, shutil, subprocess, sys, tarfile
import xml.etree.ElementTree as ET

ROOT = pathlib.Path(__file__).resolve().parents[3]
PIN = "c2f2947c3ccd8b20f7a1cdf9c3b41fb68567b6ca"
DEFAULT_CONSUMER = pathlib.Path(r"C:\Users\Rodrigo\AppData\Local\Temp\spybrowser-rpablockly-review")
PROJECTS = ("SpyBrowser.Core", "SpyBrowser.Cursory", "SpyBrowser.Playwright")

def run(cmd, cwd, *, check=True, env=None):
    print("+", " ".join(map(str, cmd)), flush=True)
    return subprocess.run(list(map(str, cmd)), cwd=cwd, check=check, env=env).returncode

def git(root, *args):
    return subprocess.check_output([shutil.which("git") or "git", "-c", f"safe.directory={root.as_posix()}", "-C", str(root), *args], text=True).strip()

def package_path(feed, package, version):
    return feed / f"{package}.{version}.nupkg"

def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--feed", type=pathlib.Path, help="consume an existing verified feed without repacking")
    ap.add_argument("--version", default="0.2.0-beta.1")
    ap.add_argument("--consumer-checkout", type=pathlib.Path, default=DEFAULT_CONSUMER)
    ap.add_argument("--output", type=pathlib.Path, default=pathlib.Path("artifacts/consumer-checks"))
    ap.add_argument("--package-only", action="store_true", help="build candidate packages only")
    args = ap.parse_args()
    out = args.output.resolve(); feed = (args.feed or out / "feed").resolve()
    out.mkdir(parents=True, exist_ok=True); feed.mkdir(parents=True, exist_ok=True)
    source_sha = git(ROOT, "rev-parse", "HEAD") if args.feed is None else "unknown"
    if args.feed is None:
        for project in PROJECTS:
            run(["dotnet", "pack", ROOT / f"src/{project}/{project}.csproj", "-c", "Release", "-p:PackageVersion=" + args.version, "-o", feed], ROOT)
    packages = [package_path(feed, project, args.version) for project in PROJECTS]
    missing = [str(p) for p in packages if not p.is_file()]
    if missing: raise RuntimeError("Candidate feed missing packages: " + ", ".join(missing))
    manifest = feed / "manifest.json"
    if args.feed is None:
        manifest_data = {"schemaVersion": 1, "sourceCommit": source_sha, "packageVersion": args.version,
            "packages": [{"id": project, "file": p.name, "sha256": hashlib.sha256(p.read_bytes()).hexdigest()} for project, p in zip(PROJECTS, packages)]}
        manifest.write_text(json.dumps(manifest_data, indent=2) + "\n", encoding="utf-8")
    else:
        if not manifest.is_file(): raise RuntimeError(f"Existing feed is not verified (missing manifest): {manifest}")
        manifest_data = json.loads(manifest.read_text(encoding="utf-8"))
        source_sha = manifest_data.get("sourceCommit", manifest_data.get("spyBrowserSourceCommit", "unknown"))
        entries = {entry.get("file"): entry.get("sha256") for entry in manifest_data.get("packages", [])}
        for package in packages:
            if entries.get(package.name) != hashlib.sha256(package.read_bytes()).hexdigest():
                raise RuntimeError(f"Existing feed package hash is absent or mismatched in manifest: {package}")
    if args.package_only:
        print(f"Preliminary package graph built from source {source_sha}: {feed}")
        return 0
    checkout = args.consumer_checkout.resolve()
    actual = git(checkout, "rev-parse", "HEAD")
    if actual != PIN: raise RuntimeError(f"Consumer checkout must be {PIN}, found {actual}: {checkout}")
    isolated = out / "consumer-checkout"
    if isolated.exists(): shutil.rmtree(isolated)
    isolated.mkdir(parents=True)
    archive = subprocess.Popen([shutil.which("git") or "git", "-c", f"safe.directory={checkout.as_posix()}", "-C", str(checkout), "archive", "--format=tar", PIN], stdout=subprocess.PIPE)
    with tarfile.open(fileobj=archive.stdout, mode="r|") as tf: tf.extractall(isolated, filter="data")
    if archive.wait(): raise RuntimeError("git archive failed")
    csproj = isolated / "src/RpaFlow.Playwright/RpaFlow.Playwright.csproj"
    text = csproj.read_text(encoding="utf-8")
    old = '<PackageReference Include="SpyBrowser.Playwright" Version="0.2.0-beta.1" />'
    if text.count(old) != 1: raise RuntimeError("Pinned SpyBrowser package reference did not match expected source")
    csproj.write_text(text.replace(old, f'<PackageReference Include="SpyBrowser.Playwright" Version="{args.version}" />'), encoding="utf-8")
    props = isolated / "Directory.Build.props"
    text = props.read_text(encoding="utf-8")
    text = text.replace("<Project>", "<Project>\n  <PropertyGroup><ManagePackageVersionsCentrally>false</ManagePackageVersionsCentrally></PropertyGroup>", 1)
    props.write_text(text, encoding="utf-8")
    (isolated / "Directory.Packages.props").write_text('<Project><PropertyGroup><ManagePackageVersionsCentrally>false</ManagePackageVersionsCentrally></PropertyGroup></Project>\n', encoding="utf-8")
    nuget = ET.Element("configuration")
    sources = ET.SubElement(nuget, "packageSources"); ET.SubElement(sources, "clear")
    ET.SubElement(sources, "add", {"key": "local-candidate", "value": str(feed)})
    ET.SubElement(sources, "add", {"key": "nuget.org", "value": "https://api.nuget.org/v3/index.json"})
    mapping = ET.SubElement(nuget, "packageSourceMapping")
    local = ET.SubElement(mapping, "packageSource", {"key": "local-candidate"}); ET.SubElement(local, "package", {"pattern": "SpyBrowser.*"})
    remote = ET.SubElement(mapping, "packageSource", {"key": "nuget.org"}); ET.SubElement(remote, "package", {"pattern": "*"})
    ET.indent(nuget, space="  ")
    ET.ElementTree(nuget).write(isolated / "NuGet.Config", encoding="utf-8", xml_declaration=True)
    tests = isolated / "tests/ConsumerContract"; tests.mkdir(parents=True)
    (tests / "ConsumerContract.csproj").write_text('''<Project Sdk="Microsoft.NET.Sdk">\n<PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net9.0</TargetFramework><ImplicitUsings>enable</ImplicitUsings><Nullable>enable</Nullable><TreatWarningsAsErrors>true</TreatWarningsAsErrors></PropertyGroup>\n<ItemGroup><ProjectReference Include="../../src/RpaFlow.Playwright/RpaFlow.Playwright.csproj" /></ItemGroup>\n</Project>\n''', encoding="utf-8")
    harness = pathlib.Path(__file__).with_name("Program.cs").read_text(encoding="utf-8")
    (tests / "Program.cs").write_text(harness, encoding="utf-8")
    evidence_path = out / "evidence.json"
    cmd = ["dotnet", "run", "--project", tests / "ConsumerContract.csproj", "-c", "Release", "--", "--output", out]
    cache = out / "nuget-packages"
    if cache.exists(): shutil.rmtree(cache)
    cache.mkdir(parents=True)
    env = os.environ.copy(); env["NUGET_PACKAGES"] = str(cache)
    resolved_sdk = subprocess.check_output(["dotnet", "--version"], cwd=isolated, text=True, env=env).strip()
    exit_code = run(cmd, isolated, check=False, env=env)
    harness_path = out / "harness-evidence.json"
    try:
        checks = json.loads(harness_path.read_text(encoding="utf-8")).get("checks", [])
    except (OSError, json.JSONDecodeError): checks = []
    incomplete = [c for c in checks if c.get("status") == "incomplete"]
    failed = [c for c in checks if c.get("status") == "failed"]
    result = "failed" if exit_code != 0 or failed else ("partial" if incomplete else "passed")
    evidence = {
      "schemaVersion": 1, "consumerCommit": PIN, "spyBrowserSourceCommit": source_sha,
      "packageIds": list(PROJECTS), "packageVersion": args.version,
      "packages": [{"path": str(p), "sha256": hashlib.sha256(p.read_bytes()).hexdigest()} for p in packages],
      "sdkRequested": "10.0.302 (rollForward latestFeature)", "sdkResolved": resolved_sdk, "consumerTargetFramework": "net9.0",
      "playwright": "1.61.0 pinned by RpaBlockly", "browser": os.environ.get("RPABLOCKLY_CHECKS_BROWSER", "Chromium (explicitly requested by harness)"),
      "executedAtUtc": dt.datetime.now(dt.timezone.utc).isoformat(), "command": list(map(str, cmd)),
      "exitCode": exit_code, "artifactDirectory": str(out), "harnessEvidence": str(harness_path),
      "result": result, "candidateFinalParity": source_sha != "unknown",
      "sourceIsolationChanges": ["SpyBrowser.Playwright package version", "Directory.Build.props modified to disable external CPM", "Directory.Packages.props added to shadow parent CPM", "NuGet.Config local feed and package source mapping", "test-only ConsumerContract project"],
      "limitations": ["Existing CAPTCHA/provider/end-to-end suite not run by this local subset.", "Upstream blocked dependencies are not inferred as passing."] + (["One or more ticket-required cases remain incomplete."] if incomplete else [])
    }
    evidence_path.write_text(json.dumps(evidence, indent=2) + "\n", encoding="utf-8")
    print(f"Evidence: {evidence_path}; aggregate result: {result}")
    return exit_code if exit_code else (2 if incomplete else 0)

if __name__ == "__main__":
    try: raise SystemExit(main())
    except Exception as ex:
        print(f"consumer-checks: {ex}", file=sys.stderr); raise
