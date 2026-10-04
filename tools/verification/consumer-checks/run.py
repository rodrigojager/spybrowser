#!/usr/bin/env python3
"""Build a local SpyBrowser package and test it from a pinned RpaBlockly checkout."""
from __future__ import annotations
import argparse, datetime as dt, json, os, pathlib, shutil, subprocess, sys, tarfile, tempfile

ROOT = pathlib.Path(__file__).resolve().parents[3]
PIN = "c2f2947c3ccd8b20f7a1cdf9c3b41fb68567b6ca"
DEFAULT_CONSUMER = pathlib.Path(r"C:\Users\Rodrigo\AppData\Local\Temp\spybrowser-rpablockly-review")

def run(cmd, cwd, *, check=True):
    print("+", " ".join(map(str, cmd)), flush=True)
    return subprocess.run(list(map(str, cmd)), cwd=cwd, check=check).returncode

def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--feed", type=pathlib.Path)
    ap.add_argument("--version", default="0.2.0-beta.1")
    ap.add_argument("--consumer-checkout", type=pathlib.Path, default=DEFAULT_CONSUMER)
    ap.add_argument("--output", type=pathlib.Path, default=pathlib.Path("artifacts/consumer-checks"))
    ap.add_argument("--package-only", action="store_true", help="build candidate into feed, do not execute consumer")
    args = ap.parse_args()
    out = args.output.resolve(); feed = (args.feed or out / "feed").resolve()
    out.mkdir(parents=True, exist_ok=True); feed.mkdir(parents=True, exist_ok=True)
    git = shutil.which("git") or "git"
    source_sha = subprocess.check_output([git, "rev-parse", "HEAD"], cwd=ROOT, text=True).strip()
    run(["dotnet", "pack", ROOT / "src/SpyBrowser.Playwright/SpyBrowser.Playwright.csproj", "-c", "Release", "-p:PackageVersion=" + args.version, "-o", feed], ROOT)
    nupkg = feed / f"SpyBrowser.Playwright.{args.version}.nupkg"
    if not nupkg.is_file(): raise RuntimeError(f"Package not produced: {nupkg}")
    if args.package_only:
        print(f"Preliminary package built from source {source_sha}: {nupkg}")
        return 0
    checkout = args.consumer_checkout.resolve()
    actual = subprocess.check_output([git, "rev-parse", "HEAD"], cwd=checkout, text=True).strip()
    if actual != PIN: raise RuntimeError(f"Consumer checkout must be {PIN}, found {actual}: {checkout}")
    isolated = out / "consumer-checkout"
    if isolated.exists(): shutil.rmtree(isolated)
    isolated.mkdir(parents=True)
    archive = subprocess.Popen([git, "archive", "--format=tar", PIN], cwd=checkout, stdout=subprocess.PIPE)
    with tarfile.open(fileobj=archive.stdout, mode="r|") as tf: tf.extractall(isolated, filter="data")
    if archive.wait(): raise RuntimeError("git archive failed")
    csproj = isolated / "src/RpaFlow.Playwright/RpaFlow.Playwright.csproj"
    text = csproj.read_text(encoding="utf-8")
    old = '<PackageReference Include="SpyBrowser.Playwright" Version="0.2.0-beta.1" />'
    if text.count(old) != 1: raise RuntimeError("Pinned SpyBrowser package reference did not match expected source")
    csproj.write_text(text.replace(old, f'<PackageReference Include="SpyBrowser.Playwright" Version="{args.version}" />'), encoding="utf-8")
    # Explicitly isolate this verification restore from machine-wide CPM settings; the pinned consumer uses versioned PackageReferences.
    props = isolated / "Directory.Build.props"
    text = props.read_text(encoding="utf-8")
    text = text.replace("<Project>", "<Project>\n  <PropertyGroup><ManagePackageVersionsCentrally>false</ManagePackageVersionsCentrally></PropertyGroup>", 1)
    props.write_text(text, encoding="utf-8")
    # This repository may itself be nested below a SpyBrowser checkout with CPM enabled; shadow that parent policy.
    (isolated / "Directory.Packages.props").write_text('<Project><PropertyGroup><ManagePackageVersionsCentrally>false</ManagePackageVersionsCentrally></PropertyGroup></Project>\n', encoding="utf-8")
    (isolated / "NuGet.Config").write_text(f'''<?xml version="1.0" encoding="utf-8"?>\n<configuration><packageSources><clear/><add key="local-candidate" value="{feed}"/><add key="nuget.org" value="https://api.nuget.org/v3/index.json" /></packageSources></configuration>\n''', encoding="utf-8")
    tests = isolated / "tests/ConsumerContract"
    tests.mkdir(parents=True)
    (tests / "ConsumerContract.csproj").write_text('''<Project Sdk="Microsoft.NET.Sdk">\n<PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net9.0</TargetFramework><ImplicitUsings>enable</ImplicitUsings><Nullable>enable</Nullable><TreatWarningsAsErrors>true</TreatWarningsAsErrors></PropertyGroup>\n<ItemGroup><ProjectReference Include="../../src/RpaFlow.Playwright/RpaFlow.Playwright.csproj" /></ItemGroup>\n</Project>\n''', encoding="utf-8")
    harness = pathlib.Path(__file__).with_name("Program.cs").read_text(encoding="utf-8")
    (tests / "Program.cs").write_text(harness, encoding="utf-8")
    evidence_path = out / "evidence.json"
    cmd = ["dotnet", "run", "--project", tests / "ConsumerContract.csproj", "-c", "Release", "--", "--output", out]
    resolved_sdk = subprocess.check_output(["dotnet", "--version"], cwd=isolated, text=True).strip()
    exit_code = run(cmd, isolated, check=False)
    evidence = {
      "schemaVersion": 1, "consumerCommit": PIN, "spyBrowserSourceCommit": source_sha,
      "packageId": "SpyBrowser.Playwright", "packageVersion": args.version,
      "packagePath": str(nupkg), "packageSha256": __import__("hashlib").sha256(nupkg.read_bytes()).hexdigest(),
      "sdkRequested": "10.0.302 (rollForward latestFeature)", "sdkResolved": resolved_sdk, "consumerTargetFramework": "net9.0",
      "playwright": "1.61.0 pinned by RpaBlockly", "browser": os.environ.get("RPABLOCKLY_CHECKS_BROWSER", "Chromium (explicitly requested by harness)"),
      "executedAtUtc": dt.datetime.now(dt.timezone.utc).isoformat(), "command": cmd,
      "exitCode": exit_code, "artifactDirectory": str(out), "harnessEvidence": str(out / "harness-evidence.json"),
      "result": "passed" if exit_code == 0 else "failed", "candidateFinalParity": False,
      "sourceIsolationChanges": ["SpyBrowser.Playwright package version", "NuGet.Config local feed", "test-only ConsumerContract project"],
      "limitations": ["This is preliminary evidence for the unintegrated source commit; rerun against final candidate for parity.", "Existing CAPTCHA/provider/end-to-end suite not run by this local subset.", "Pending upstream dependencies remain separate incomplete checks."]
    }
    evidence_path.write_text(json.dumps(evidence, indent=2, default=str) + "\n", encoding="utf-8")
    print(f"Evidence: {evidence_path}")
    return exit_code

if __name__ == "__main__":
    try: raise SystemExit(main())
    except Exception as ex:
        print(f"consumer-checks: {ex}", file=sys.stderr); raise
