#!/usr/bin/env python3
"""Build and run an isolated, feed-only historical-vs-candidate movement consumer."""
from __future__ import annotations
import argparse, hashlib, json, os, pathlib, shutil, subprocess, sys, tarfile, xml.etree.ElementTree as ET, zipfile

ROOT = pathlib.Path(__file__).resolve().parents[2]
BASELINE = "e217359d19a29635f2b3b5ba54664d299fd16d36"
PRELIM_CANDIDATE = "d79c085ca4f6e349ad5b0ed744158653ee6382b6"
BASE_PACKAGES = ("SpyBrowser.Core", "SpyBrowser.Playwright")
CANDIDATE_PACKAGES = ("SpyBrowser.Core", "SpyBrowser.Cursory", "SpyBrowser.Playwright")

def sha(data: bytes) -> str: return hashlib.sha256(data).hexdigest()
def git_prefix(root: pathlib.Path) -> list[str]:
    marker = root / ".git"
    if os.name != "nt" and marker.is_file():
        value = marker.read_text(encoding="utf-8").strip().replace("gitdir:", "", 1).strip().replace("\\", "/")
        if len(value) > 2 and value[1:3] == ":/": value = f"/mnt/{value[0].lower()}/{value[3:]}"
        return ["--git-dir=" + str(pathlib.Path(value)), "--work-tree=" + str(root)]
    return ["-C", str(root)]
def git(root: pathlib.Path, *args: str) -> str: return subprocess.check_output(["git", *git_prefix(root), *args], text=True).strip()
def tree_sha(root: pathlib.Path, revision: str) -> str: return git(root, "rev-parse", revision + "^{tree}")
def run(cmd: list[str], cwd: pathlib.Path, env=None) -> None:
    print("+", " ".join(map(str, cmd)), flush=True)
    subprocess.run(cmd, cwd=cwd, env=env, check=True)
def archive(root: pathlib.Path, revision: str, destination: pathlib.Path) -> str:
    data = subprocess.check_output(["git", *git_prefix(root), "archive", "--format=tar", revision])
    digest = sha(data)
    destination.mkdir(parents=True, exist_ok=True)
    with tarfile.open(fileobj=__import__("io").BytesIO(data), mode="r:") as tar:
        if hasattr(tarfile, "data_filter"): tar.extractall(destination, filter="data")
        else: tar.extractall(destination)
    return digest
def package_version(revision: str, suffix: str) -> str:
    return f"0.2.0-historical.{revision[:8]}.{suffix}"
def build_feed(source: pathlib.Path, rev: str, archive_sha: str, feed: pathlib.Path, ids: tuple[str, ...], suffix: str) -> dict:
    feed.mkdir(parents=True, exist_ok=True)
    version = package_version(rev, suffix)
    env = os.environ.copy(); env["NUGET_PACKAGES"] = str(feed.parent / "pack-cache")
    for package in ids:
        run(["dotnet", "pack", source / f"src/{package}/{package}.csproj", "-c", "Release", "-p:PackageVersion=" + version, "-p:RepositoryCommit=" + rev, "-p:ContinuousIntegrationBuild=true", "-o", str(feed)], source, env)
    packages = []
    for package in ids:
        path = feed / f"{package}.{version}.nupkg"
        if not path.is_file(): raise RuntimeError(f"Package was not created: {path}")
        packages.append({"id": package, "file": path.name, "sha256": sha(path.read_bytes())})
    manifest = {"schemaVersion": 1, "sourceCommit": rev, "sourceTreeSha": tree_sha(source, rev), "sourceArchiveSha256": archive_sha, "packageVersion": version, "packages": packages, "feedRole": suffix}
    path = feed / "manifest.json"; path.write_text(json.dumps(manifest, indent=2) + "\n", encoding="utf-8")
    return {"path": feed, "version": version, "manifest": path, "manifestSha256": sha(path.read_bytes()), "sourceCommit": rev, "sourceTreeSha": tree_sha(source, rev), "sourceArchiveSha256": archive_sha, "packageIds": ids}
def check_feed(feed: pathlib.Path, expected_ids: tuple[str, ...], expected_rev: str | None, source_root: pathlib.Path):
    path = feed / "manifest.json"
    if not path.is_file(): raise RuntimeError(f"NuGet feed manifest is required: {path}")
    manifest = json.loads(path.read_text(encoding="utf-8"))
    source_commit = manifest.get("sourceCommit")
    if expected_rev and source_commit != expected_rev: raise RuntimeError(f"Feed sourceCommit must equal {expected_rev}, got {source_commit}")
    archive_hash = manifest.get("sourceArchiveSha256", "")
    if not source_commit or len(archive_hash) != 64 or any(c not in "0123456789abcdefABCDEF" for c in archive_hash): raise RuntimeError("Feed must bind a source commit and complete producer git-archive SHA-256")
    actual_tree_sha = tree_sha(source_root, source_commit)
    if actual_tree_sha != manifest.get("sourceTreeSha"): raise RuntimeError(f"Feed source tree hash mismatch for {source_commit}: expected {manifest.get('sourceTreeSha')}, got {actual_tree_sha}")
    if tuple(item.get("id") for item in manifest.get("packages", [])) != expected_ids: raise RuntimeError(f"Manifest package ids/order must be exactly {expected_ids}")
    declared_files = set()
    for item in manifest["packages"]:
        filename = item.get("file", "")
        if pathlib.Path(filename).name != filename: raise RuntimeError(f"Invalid package filename in feed manifest: {filename}")
        package = feed / filename; declared_files.add(package.name)
        if not package.is_file() or sha(package.read_bytes()) != item.get("sha256"): raise RuntimeError(f"Missing/mismatched feed package hash: {package}")
        with zipfile.ZipFile(package) as archive_file:
            nuspec_name = next((name for name in archive_file.namelist() if name.endswith(".nuspec")), None)
            if nuspec_name is None: raise RuntimeError(f"Package has no nuspec: {package}")
            nuspec = ET.fromstring(archive_file.read(nuspec_name))
            metadata = nuspec.find(".//{*}metadata")
            repository = metadata.find("{*}repository") if metadata is not None else None
            if metadata is None or metadata.findtext("{*}version") != manifest.get("packageVersion") or repository is None or repository.get("commit") != source_commit:
                raise RuntimeError(f"Package nuspec source/version does not match feed manifest: {package}")
    actual_files = {p.name for p in feed.glob("*.nupkg")}
    if actual_files != declared_files: raise RuntimeError(f"Feed nupkg set differs from manifest: {actual_files} != {declared_files}")
    return {"path": feed, "version": manifest["packageVersion"], "manifest": path, "manifestSha256": sha(path.read_bytes()), "sourceCommit": manifest["sourceCommit"], "sourceTreeSha": manifest["sourceTreeSha"], "sourceArchiveSha256": manifest["sourceArchiveSha256"], "packageIds": expected_ids}
def make_nuget_config(path: pathlib.Path, feed: pathlib.Path):
    config = ET.Element("configuration"); sources = ET.SubElement(config, "packageSources"); ET.SubElement(sources, "clear")
    ET.SubElement(sources, "add", {"key": "verified-feed", "value": str(feed.resolve())}); ET.SubElement(sources, "add", {"key": "nuget.org", "value": "https://api.nuget.org/v3/index.json"})
    mapping = ET.SubElement(config, "packageSourceMapping"); local = ET.SubElement(mapping, "packageSource", {"key": "verified-feed"}); ET.SubElement(local, "package", {"pattern": "SpyBrowser.*"}); remote = ET.SubElement(mapping, "packageSource", {"key": "nuget.org"}); ET.SubElement(remote, "package", {"pattern": "*"})
    if hasattr(ET, "indent"): ET.indent(config, space="  ")
    ET.ElementTree(config).write(path, encoding="utf-8", xml_declaration=True)
def validate_consumer_assets(obj: pathlib.Path, version: str, package_ids: tuple[str, ...]):
    lock = json.loads((obj / "project.assets.json").read_text(encoding="utf-8"))
    package_keys = lock.get("libraries", {})
    for package in package_ids:
        matches = [key for key, value in package_keys.items() if key.lower().startswith(package.lower() + "/") and value.get("type") == "package"]
        if matches != [f"{package}/{version}"]: raise RuntimeError(f"Consumer did not resolve {package} solely to feed version {version}: {matches}")
    if any(value.get("type") == "project" and key.lower().startswith("spybrowser.") for key, value in package_keys.items()): raise RuntimeError("SpyBrowser project dependency entered consumer graph; expected NuGet-only packages")
def consume(feed, variant: str, output: pathlib.Path):
    project = output / "consumer"; project.mkdir(parents=True, exist_ok=True)
    version = feed["version"]
    make_nuget_config(project / "NuGet.Config", feed["path"])
    csproj = f'''<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><ManagePackageVersionsCentrally>false</ManagePackageVersionsCentrally><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><ImplicitUsings>enable</ImplicitUsings><Nullable>enable</Nullable><TreatWarningsAsErrors>true</TreatWarningsAsErrors></PropertyGroup><ItemGroup><PackageReference Include="SpyBrowser.Playwright" Version="{version}" />''' + (f'<PackageReference Include="SpyBrowser.Cursory" Version="{version}" />' if "Cursory" in feed["packageIds"] else "") + '''<PackageReference Include="Microsoft.Playwright" Version="1.61.0" /></ItemGroup></Project>'''
    (project / "Consumer.csproj").write_text(csproj, encoding="utf-8")
    shutil.copy2(pathlib.Path(__file__).with_name("Consumer.cs"), project / "Program.cs")
    cache = output / "nuget-cache"; cache.mkdir(parents=True, exist_ok=True)
    env = os.environ.copy(); env.update({"NUGET_PACKAGES": str(cache), "MOTION_SOURCE_SHA": feed["sourceCommit"], "MOTION_PACKAGE_VERSION": version, "MOTION_FEED_MANIFEST": str(feed["manifest"].resolve()), "MOTION_FEED_MANIFEST_SHA256": feed["manifestSha256"], "MOTION_SOURCE_ARCHIVE_SHA256": feed["sourceArchiveSha256"], "MOTION_SOURCE_TREE_SHA256": feed["sourceTreeSha"]})
    run(["dotnet", "restore", project / "Consumer.csproj", "--configfile", project / "NuGet.Config"], project, env)
    validate_consumer_assets(project / "obj", version, feed["packageIds"])
    run(["dotnet", "run", "--project", project / "Consumer.csproj", "-c", "Release", "--no-restore", "--", variant, str(output)], project, env)

def main():
    ap = argparse.ArgumentParser(description=__doc__)
    ap.add_argument("--output", type=pathlib.Path, default=pathlib.Path("artifacts/historical-motion-comparison"))
    ap.add_argument("--candidate-feed", type=pathlib.Path, help="later parent-approved final feed; manifest/package hashes are mandatory")
    ap.add_argument("--build-preliminary-candidate", action="store_true", help="pack exact current d79c085 archive into an explicitly preliminary feed")
    ap.add_argument("--skip-run", action="store_true", help="only produce preliminary candidate NuGet feed")
    ap.add_argument("--baseline-feed", type=pathlib.Path, help="optional previously built exact-e217 feed")
    args = ap.parse_args()
    output = args.output.resolve(); output.mkdir(parents=True, exist_ok=True)
    baseline_root = output / "source-baseline"
    baseline_sha = archive(ROOT, BASELINE, baseline_root)
    baseline_feed = check_feed(args.baseline_feed.resolve(), BASE_PACKAGES, BASELINE, ROOT) if args.baseline_feed else build_feed(baseline_root, BASELINE, baseline_sha, output / "feed-baseline", BASE_PACKAGES, "e217359d")
    if args.candidate_feed:
        candidate_feed = check_feed(args.candidate_feed.resolve(), CANDIDATE_PACKAGES, None, ROOT)
    else:
        if not args.build_preliminary_candidate: raise RuntimeError("Provide --candidate-feed or explicitly request --build-preliminary-candidate")
        current = git(ROOT, "rev-parse", "HEAD")
        if current != PRELIM_CANDIDATE: raise RuntimeError(f"Preliminary feed requires exact candidate source {PRELIM_CANDIDATE}; current HEAD is {current}")
        candidate_root = output / "source-preliminary-candidate"
        candidate_sha = archive(ROOT, PRELIM_CANDIDATE, candidate_root)
        candidate_feed = build_feed(candidate_root, PRELIM_CANDIDATE, candidate_sha, output / "feed-candidate-preliminary", CANDIDATE_PACKAGES, "preliminary-d79c085")
    summary = {"schemaVersion": 1, "baseline": {k: str(v) if isinstance(v, pathlib.Path) else v for k, v in baseline_feed.items()}, "candidate": {k: str(v) if isinstance(v, pathlib.Path) else v for k, v in candidate_feed.items()}, "status": "preliminary candidate packages; final candidate feed not supplied" if "preliminary" in candidate_feed["version"] else "feed hashes verified; run results pending"}
    (output / "feed-summary.json").write_text(json.dumps(summary, indent=2) + "\n", encoding="utf-8")
    print(f"Feed summary: {output / 'feed-summary.json'}")
    if args.skip_run: return
    variants = [(baseline_feed, "baseline-bezier", "baseline-bezier"), (candidate_feed, "candidate-bezier", "candidate-bezier"), (candidate_feed, "candidate-cursory", "candidate-cursory"), (candidate_feed, "candidate-off", "candidate-off")]
    for feed, variant, folder in variants: consume(feed, variant, output / folder)
    (output / "RUN-MEANING.md").write_text("Historical and candidate package builds were consumed through isolated NuGet-only projects and separate NUGET_PACKAGES caches. Baseline Bezier is compared with candidate Bezier/Cursory/off; task completion is a functional result, timing is descriptive. The historical RNG is not seedable; no paired trajectory is claimed. A preliminary candidate feed is not final-package evidence. Review each result.json and compare environment metadata before making any regression claim.\n", encoding="utf-8")
    print(f"Completed isolated consumer runs: {output}")
if __name__ == "__main__":
    try: main()
    except Exception as exc: print(f"historical-motion-comparison: {exc}", file=sys.stderr); raise
