#!/usr/bin/env python3
"""Build or consume a local SpyBrowser feed and run the pinned RpaBlockly subset."""
from __future__ import annotations
import argparse, datetime as dt, hashlib, json, os, pathlib, shutil, subprocess, sys, tarfile
import xml.etree.ElementTree as ET
import zipfile

ROOT = pathlib.Path(__file__).resolve().parents[3]
PIN = "c2f2947c3ccd8b20f7a1cdf9c3b41fb68567b6ca"
DEFAULT_CONSUMER = pathlib.Path(r"C:\Users\Rodrigo\AppData\Local\Temp\spybrowser-rpablockly-review")
PROJECTS = ("SpyBrowser.Core", "SpyBrowser.Cursory", "SpyBrowser.Playwright")
REQUIRED_CHECKS = {
    "Configured Humanize state, Chromium launch via pinned RpaBlockly BrowserLauncher, in-memory rpablockly identity, locale, timezone, viewport and context event/collection",
    "StorageStatePath write on local loopback origin",
    "Screenshot, navigation and local loopback served page",
    "RpaBlockly popup plus nested-frame wrapper propagation and working loopback page",
    "Pinned RpaBlockly V1 flow: parsed definition, FlowCompiler, real RpaRunner loopback execution and frame output via IFrameLocator/ILocator",
    "Pinned RpaBlockly V2 package flow: parsed snapshot, V2FlowCompiler, real RpaRunner loopback execution and frame output via IFrameLocator/ILocator",
    "Loopback download and saved artifact",
    "Actual RpaRunner local flow restores StorageStatePath cookies",
    "Actual RpaRunner FillWhenReadyAsync -> FillWithRuntimeAsync cancellation closes context and settles native Fill",
    "Two concurrent actual RpaRunner jobs retain independent inputs and deterministic Cursory RNG trajectories without profile leases",
    "Actual RpaBlockly cancellation helper cleans a controlled late BrowserSession (native IsConnected=false)",
    "Context close removes collection entry",
    "Humanize off remains raw",
}

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
    ap.add_argument("--expected-source-commit", help="required integrated SpyBrowser commit for final-parity evidence")
    ap.add_argument("--mouse-algorithm", choices=("cursory", "bezier"), default="cursory")
    ap.add_argument("--compatibility-mode", choices=("playwright-compatible", "legacy"), default="playwright-compatible")
    ap.add_argument("--humanize", choices=("on", "off"), default="on")
    args = ap.parse_args()
    out = args.output.resolve(); feed = (args.feed or out / "feed").resolve()
    out.mkdir(parents=True, exist_ok=True); feed.mkdir(parents=True, exist_ok=True)
    source_sha = git(ROOT, "rev-parse", "HEAD") if args.feed is None else "unknown"
    package_build_commands = []
    if args.feed is None:
        for project in PROJECTS:
            pack_command = ["dotnet", "pack", ROOT / f"src/{project}/{project}.csproj", "-c", "Release", "-p:PackageVersion=" + args.version, "-o", feed]
            package_build_commands.append(list(map(str, pack_command)))
            run(pack_command, ROOT)
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
        if set(entries) != {package.name for package in packages}:
            raise RuntimeError("Existing feed manifest must declare exactly the three candidate packages")
        for package in packages:
            if entries.get(package.name) != hashlib.sha256(package.read_bytes()).hexdigest():
                raise RuntimeError(f"Existing feed package hash is absent or mismatched in manifest: {package}")
    package_source_commits = {}
    for package in packages:
        with zipfile.ZipFile(package) as archive_file:
            nuspec = ET.fromstring(archive_file.read(next(name for name in archive_file.namelist() if name.endswith(".nuspec"))))
        repository = nuspec.find(".//{*}repository")
        package_source_commits[package.name] = repository.get("commit") if repository is not None else None
    declared_final_commit = manifest_data.get("declaredFinalCommit")
    final_parity = bool(args.expected_source_commit and declared_final_commit == args.expected_source_commit and
                        source_sha == args.expected_source_commit and
                        all(commit == args.expected_source_commit for commit in package_source_commits.values()))
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
    launcher = isolated / "src/RpaFlow.Playwright/Core/BrowserLauncher.cs"
    launcher_text = launcher.read_text(encoding="utf-8")
    launch_options = '''                        RunGpuProbe = false
'''
    configured_options = f'''                        RunGpuProbe = false,
                        HumanInteraction = new HumanInteractionOptions
                        {{
                            MouseAlgorithm = MouseTrajectoryAlgorithm.{"Cursory" if args.mouse_algorithm == "cursory" else "Bezier"},
                            CompatibilityMode = HumanizationCompatibilityMode.{"PlaywrightCompatible" if args.compatibility_mode == "playwright-compatible" else "Legacy"}
                        }}
'''
    if launcher_text.count(launch_options) != 1:
        raise RuntimeError("Pinned BrowserLauncher initialization did not match the expected configuration anchor")
    launcher.write_text(launcher_text.replace(launch_options, configured_options), encoding="utf-8")
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
    (tests / "ConsumerContract.csproj").write_text('''<Project Sdk="Microsoft.NET.Sdk">\n<PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net9.0</TargetFramework><ImplicitUsings>enable</ImplicitUsings><Nullable>enable</Nullable><TreatWarningsAsErrors>true</TreatWarningsAsErrors></PropertyGroup>\n<ItemGroup><ProjectReference Include="../../src/RpaFlow.Playwright/RpaFlow.Playwright.csproj" /><ProjectReference Include="../RpaFlow.Legacy.Playwright/RpaFlow.Legacy.Playwright.csproj" /><Content Include="fixtures/**/*.json" CopyToOutputDirectory="PreserveNewest" /></ItemGroup>\n</Project>\n''', encoding="utf-8")
    shutil.copytree(pathlib.Path(__file__).with_name("fixtures"), tests / "fixtures")
    harness = pathlib.Path(__file__).with_name("Program.cs").read_text(encoding="utf-8")
    (tests / "Program.cs").write_text(harness, encoding="utf-8")
    evidence_path = out / "evidence.json"
    cmd = ["dotnet", "run", "--project", tests / "ConsumerContract.csproj", "-c", "Release", "--", "--output", out,
           "--humanize", args.humanize, "--mouse-algorithm", args.mouse_algorithm,
           "--compatibility-mode", args.compatibility_mode]
    cache = out / "nuget-packages"
    if cache.exists(): shutil.rmtree(cache)
    cache.mkdir(parents=True)
    env = os.environ.copy()
    env["NUGET_PACKAGES"] = str(cache)
    env["DOTNET_CLI_HOME"] = str(out / "dotnet-cli-home")
    env["DOTNET_MULTILEVEL_LOOKUP"] = "0"
    pathlib.Path(env["DOTNET_CLI_HOME"]).mkdir(parents=True, exist_ok=True)
    resolved_sdk = subprocess.check_output(["dotnet", "--version"], cwd=isolated, text=True, env=env).strip()
    exit_code = run(cmd, isolated, check=False, env=env)
    harness_path = out / "harness-evidence.json"
    try:
        checks = json.loads(harness_path.read_text(encoding="utf-8")).get("checks", [])
    except (OSError, json.JSONDecodeError): checks = []
    failed = [c for c in checks if c.get("status") == "failed"]
    passed_checks = [c for c in checks if c.get("status") == "passed"]
    passed_names = [c.get("name") for c in passed_checks]
    missing_required_names = sorted(REQUIRED_CHECKS - set(passed_names))
    unexpected_passed_names = sorted(set(passed_names) - REQUIRED_CHECKS)
    required_count = len(REQUIRED_CHECKS)
    missing_required = len(missing_required_names)
    result = "failed" if exit_code != 0 or failed or unexpected_passed_names else ("partial" if missing_required else "passed")
    evidence = {
      "schemaVersion": 1, "consumerCommit": PIN, "spyBrowserSourceCommit": source_sha,
      "packageIds": list(PROJECTS), "packageVersion": args.version,
      "packages": [{"path": str(p), "sha256": hashlib.sha256(p.read_bytes()).hexdigest()} for p in packages],
      "sdkRequested": "10.0.302 (rollForward latestFeature)", "sdkResolved": resolved_sdk, "consumerTargetFramework": "net9.0",
      "isolationEnvironment": {"NUGET_PACKAGES": str(cache), "DOTNET_CLI_HOME": env["DOTNET_CLI_HOME"], "DOTNET_MULTILEVEL_LOOKUP": env["DOTNET_MULTILEVEL_LOOKUP"]},
      "playwright": "1.61.0 pinned by RpaBlockly", "browser": os.environ.get("RPABLOCKLY_CHECKS_BROWSER", "Chromium (explicitly requested by harness)"),
      "executedAtUtc": dt.datetime.now(dt.timezone.utc).isoformat(), "driverCommand": [sys.executable, str(pathlib.Path(__file__).resolve()), *sys.argv[1:]],
      "packageBuildCommands": package_build_commands, "consumerCommand": list(map(str, cmd)),
      "exitCode": exit_code, "artifactDirectory": str(out), "harnessEvidence": str(harness_path),
      "result": result, "candidateFinalParity": final_parity,
      "expectedFinalSourceCommit": args.expected_source_commit, "declaredFinalCommit": declared_final_commit,
      "packageSourceCommits": package_source_commits, "humanize": args.humanize,
      "mouseAlgorithm": args.mouse_algorithm, "compatibilityMode": args.compatibility_mode,
      "requiredLocalChecksPassed": len(passed_checks), "requiredLocalChecksExpected": required_count,
      "requiredLocalCheckNames": sorted(REQUIRED_CHECKS), "missingRequiredLocalCheckNames": missing_required_names,
      "unexpectedPassedLocalCheckNames": unexpected_passed_names,
      "excludedCoverage": [{"name": "Full mixed CAPTCHA/provider/end-to-end suite", "status": "not-run", "reason": "Explicitly excluded; not part of the mandatory local subset."}],
      "sourceIsolationChanges": ["SpyBrowser.Playwright package version", "isolated BrowserLauncher HumanInteraction configuration only", "Directory.Build.props modified to disable external CPM", "Directory.Packages.props added to shadow parent CPM", "NuGet.Config local feed and package source mapping", "test-only ConsumerContract project referencing the pinned existing RpaFlow.Legacy.Playwright project for the excluded V1 route"],
      "limitations": ["Existing CAPTCHA/provider/end-to-end suite not run by this local subset.", "Final package parity is false unless expected commit, manifest declaredFinalCommit, source SHA, and all package repository commits match."] + (["Missing required checks: " + ", ".join(missing_required_names)] if missing_required else []) + (["Unexpected passed check names: " + ", ".join(unexpected_passed_names)] if unexpected_passed_names else [])
    }
    evidence_path.write_text(json.dumps(evidence, indent=2) + "\n", encoding="utf-8")
    print(f"Evidence: {evidence_path}; aggregate result: {result}")
    return exit_code if exit_code else (2 if missing_required else 0)

if __name__ == "__main__":
    try: raise SystemExit(main())
    except Exception as ex:
        print(f"consumer-checks: {ex}", file=sys.stderr); raise
