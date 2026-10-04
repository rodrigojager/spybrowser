#!/usr/bin/env python3
"""Run package-only candidate/rollback checks from an isolated external consumer directory."""
import argparse
import hashlib
import json
import os
import shutil
import subprocess
import sys
import tempfile
import zipfile
import xml.etree.ElementTree as ET
from xml.sax.saxutils import quoteattr
from pathlib import Path

BASELINE = "e217359d19a29635f2b3b5ba54664d299fd16d36"
PLAYWRIGHT = "1.61.0"


def sha(path: Path) -> str:
    h = hashlib.sha256()
    with path.open("rb") as f:
        for block in iter(lambda: f.read(1024 * 1024), b""):
            h.update(block)
    return h.hexdigest()


def run(command, *, cwd, env, log):
    result = subprocess.run(command, cwd=cwd, env=env, text=True, stdout=subprocess.PIPE, stderr=subprocess.STDOUT, timeout=240)
    log.append({"command": [str(x) for x in command], "cwd": str(cwd), "exitCode": result.returncode, "output": result.stdout})
    if result.returncode:
        raise RuntimeError(f"Command failed ({result.returncode}): {' '.join(map(str, command))}\n{result.stdout}")
    return result.stdout


def git_archive(repository: Path) -> bytes:
    result = subprocess.run(["git", "-c", f"safe.directory={repository.as_posix()}", "-C", str(repository), "archive", BASELINE],
                            stdout=subprocess.PIPE, stderr=subprocess.PIPE)
    if result.returncode:
        raise RuntimeError(f"Cannot read baseline {BASELINE}: {result.stderr.decode(errors='replace')}")
    return result.stdout


def verify_previous_feed(feed: Path, manifest_path: Path, repository: Path, version: str) -> dict:
    """Fail closed unless the supplied packages are byte-proven from the pinned Git baseline."""
    try:
        manifest = json.loads(manifest_path.read_text(encoding="utf-8"))
    except (OSError, json.JSONDecodeError) as exc:
        raise RuntimeError(f"Cannot read previous-feed provenance manifest {manifest_path}: {exc}") from exc
    if not isinstance(manifest, dict):
        raise RuntimeError("Previous-feed manifest must be a JSON object")
    if manifest.get("sourceCommit") != BASELINE or manifest.get("version") != version:
        raise RuntimeError("Previous-feed manifest must identify the exact pinned baseline commit and --previous-version")
    expected_ids = ("SpyBrowser.Core", "SpyBrowser.Playwright")
    hashes = manifest.get("packageHashes")
    expected_names = {f"{package_id}.{version}.nupkg" for package_id in expected_ids}
    if not isinstance(hashes, dict) or set(hashes) != expected_names or any(not isinstance(v, str) or len(v) != 64 for v in hashes.values()):
        raise RuntimeError("Previous-feed manifest packageHashes must contain exactly the two pinned package filenames and SHA256 hashes")
    actual_names = {p.name for p in feed.glob("*.nupkg") if not p.name.endswith(".snupkg")}
    if actual_names != expected_names:
        raise RuntimeError("Previous feed must contain exactly SpyBrowser.Core and SpyBrowser.Playwright for --previous-version")
    for filename in sorted(expected_names):
        package = feed / filename
        if sha(package) != hashes[filename]:
            raise RuntimeError(f"Previous-feed package hash mismatch: {filename}")
        try:
            with zipfile.ZipFile(package) as archive:
                nuspecs = [name for name in archive.namelist() if name.endswith(".nuspec")]
                if len(nuspecs) != 1:
                    raise RuntimeError(f"Expected one nuspec in {filename}")
                root = ET.fromstring(archive.read(nuspecs[0]))
                ns_uri = root.tag.partition("}")[0].lstrip("{")
                ns = {"n": ns_uri} if ns_uri else {}
                metadata = root.find("n:metadata", ns) if ns else root.find("metadata")
                if metadata is None:
                    raise RuntimeError(f"Missing nuspec metadata in {filename}")
                repository_node = metadata.find("n:repository", ns) if ns else metadata.find("repository")
                package_id = metadata.findtext("n:id", default="", namespaces=ns) if ns else metadata.findtext("id", default="")
                package_version = metadata.findtext("n:version", default="", namespaces=ns) if ns else metadata.findtext("version", default="")
                expected_id = next(package_id for package_id in expected_ids if filename == f"{package_id}.{version}.nupkg")
                if package_id != expected_id or package_version != version or repository_node is None or repository_node.attrib.get("commit") != BASELINE:
                    raise RuntimeError(f"Repository metadata mismatch in {filename}; expected package identity/version and commit {BASELINE}")
        except (OSError, zipfile.BadZipFile, ET.ParseError) as exc:
            raise RuntimeError(f"Invalid package metadata in {filename}: {exc}") from exc
    try:
        archive = git_archive(repository)
    except RuntimeError as exc:
        raise RuntimeError(f"Cannot verify previous feed against repository baseline: {exc}") from exc
    archive_hash = hashlib.sha256(archive).hexdigest()
    if manifest.get("sourceArchiveSha256") != archive_hash:
        raise RuntimeError("Previous-feed sourceArchiveSha256 does not match a fresh git archive of the pinned baseline")
    return manifest


def package_manifest(feeds):
    rows = []
    for feed in feeds:
        for p in sorted(feed.glob("*.nupkg")):
            if p.name.endswith(".snupkg"):
                continue
            with zipfile.ZipFile(p) as z:
                names = z.namelist()
                nuspecs = [n for n in names if n.endswith(".nuspec")]
                nuspec_metadata = {}
                for name in nuspecs:
                    root = ET.fromstring(z.read(name))
                    namespace = root.tag.partition("}")[0].lstrip("{")
                    ns = {"n": namespace} if namespace else {}
                    metadata = root.find("n:metadata", ns) if ns else root.find("metadata")
                    deps = metadata.find("n:dependencies", ns) if metadata is not None and ns else (metadata.find("dependencies") if metadata is not None else None)
                    dependency_ids = [] if deps is None else [item.attrib.get("id", "") for item in deps.iter() if item.tag.endswith("dependency")]
                    nuspec_metadata[name] = {"version": metadata.findtext("n:version", default="", namespaces=ns) if ns else metadata.findtext("version", default=""),
                                             "license": metadata.findtext("n:license", default="", namespaces=ns) if ns else metadata.findtext("license", default=""),
                                             "dependencyIds": dependency_ids}
                rows.append({"feed": str(feed), "file": p.name, "sha256": sha(p), "nuspec": nuspecs,
                             "nuspecMetadata": nuspec_metadata,
                             "nuspecSha256": {n: hashlib.sha256(z.read(n)).hexdigest() for n in nuspecs},
                             "runtimeAssemblies": {n: hashlib.sha256(z.read(n)).hexdigest() for n in names if n.endswith(".dll") and n.startswith("lib/")},
                             "licenseAndSourceEntries": {n: hashlib.sha256(z.read(n)).hexdigest() for n in names if "license" in n.lower() or n.startswith("source/") or "notice" in n.lower()},
                             "symbols": next(({"file": p.with_name(p.stem + ".snupkg").name,
                                                "sha256": sha(p.with_name(p.stem + ".snupkg"))}
                                               for _ in [0] if p.with_name(p.stem + ".snupkg").exists()), None)})
    return rows


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--feed", type=Path, required=True, help="Existing final local NuGet feed; never repacked from source")
    ap.add_argument("--candidate-version", required=True)
    ap.add_argument("--expected-source-commit", required=True, help="Source commit recorded by the feed manifest")
    ap.add_argument("--declared-final-commit", help="Required only for a genuinely final feed; preliminary manifests may declare no final commit")
    ap.add_argument("--feed-manifest", type=Path, required=True, help="Existing provenance manifest for the candidate feed")
    ap.add_argument("--previous-feed", type=Path)
    ap.add_argument("--previous-feed-manifest", type=Path, help="Provenance manifest for a caller-supplied baseline feed; requires --repository")
    ap.add_argument("--previous-version", default="0.1.0-baseline.e217359")
    ap.add_argument("--dependency-feed", type=Path, action="append", default=[], help="Additional local-only feed for exact pinned transitive packages such as Microsoft.Playwright")
    ap.add_argument("--output", type=Path, required=True)
    ap.add_argument("--evidence-input", action="append", default=[], help="Evidence artifact paths to associate; not asserted as passing")
    ap.add_argument("--repository", type=Path, help="Repository containing pinned baseline for optional previous-package build")
    args = ap.parse_args()
    feed = args.feed.resolve()
    output = args.output.resolve()
    output.mkdir(parents=True, exist_ok=True)
    if not feed.is_dir() or not any(feed.glob("SpyBrowser.Playwright.*.nupkg")):
        ap.error(f"Existing candidate feed must contain SpyBrowser.Playwright nupkg: {feed}")
    manifest_path = args.feed_manifest.resolve()
    if not manifest_path.is_file():
        ap.error(f"Required existing feed manifest is missing: {manifest_path}")
    try:
        provenance = json.loads(manifest_path.read_text(encoding="utf-8"))
    except (OSError, json.JSONDecodeError) as exc:
        ap.error(f"Cannot read feed provenance manifest {manifest_path}: {exc}")
    if provenance.get("candidateVersion") != args.candidate_version:
        ap.error("Feed manifest candidateVersion does not match --candidate-version")
    if provenance.get("sourceCommit") != args.expected_source_commit:
        ap.error("Feed manifest sourceCommit does not match --expected-source-commit")
    if provenance.get("declaredFinalCommit") != args.declared_final_commit:
        ap.error("Feed manifest declaredFinalCommit does not match --declared-final-commit")
    is_final = provenance.get("isFinal", True)
    if not isinstance(is_final, bool):
        ap.error("Feed manifest isFinal must be a boolean")
    if is_final and (not args.declared_final_commit or args.declared_final_commit != args.expected_source_commit):
        ap.error("A final feed must declare its exact verified source commit as final")
    declared_hashes = provenance.get("packages")
    if not isinstance(declared_hashes, dict) or not declared_hashes:
        ap.error("Feed manifest must contain packages as {package filename: sha256}")
    actual_candidate_packages = {p.name: sha(p) for p in sorted(feed.glob("*.nupkg")) if not p.name.endswith(".snupkg")}
    for filename, digest in actual_candidate_packages.items():
        if declared_hashes.get(filename) != digest:
            ap.error(f"Feed manifest hash missing/mismatched for {filename}")
    if set(declared_hashes) != set(actual_candidate_packages):
        ap.error("Feed manifest package list does not exactly match candidate .nupkg files")
    logs = []
    statuses = []
    previous_feed = args.previous_feed.resolve() if args.previous_feed else None
    if args.previous_feed_manifest and (previous_feed is None or args.repository is None):
        ap.error("--previous-feed-manifest requires both --previous-feed and --repository")
    if args.previous_feed_manifest and not args.previous_feed_manifest.resolve().is_file():
        ap.error(f"Previous-feed provenance manifest is missing: {args.previous_feed_manifest.resolve()}")
    if args.previous_version == args.candidate_version:
        ap.error("Previous and candidate package versions must be distinct")
    for package_id in ("SpyBrowser.Core", "SpyBrowser.Cursory", "SpyBrowser.Playwright"):
        if not (feed / f"{package_id}.{args.candidate_version}.nupkg").is_file():
            ap.error(f"Candidate feed is missing exact package {package_id}.{args.candidate_version}.nupkg")
    with tempfile.TemporaryDirectory(prefix="spybrowser-distribution-check-") as temp_s:
        temp = Path(temp_s)
        if previous_feed is None:
            if args.repository is None:
                ap.error("Supply --previous-feed or --repository to build pinned baseline e217 artifact")
            baseline_tree, previous_feed = temp / "baseline-source", temp / "previous-feed"
            baseline_tree.mkdir(); previous_feed.mkdir()
            try:
                archive_bytes = git_archive(args.repository.resolve())
            except RuntimeError as exc:
                ap.error(str(exc))
            import tarfile, io
            with tarfile.open(fileobj=io.BytesIO(archive_bytes), mode="r:") as tar:
                if sys.version_info >= (3, 12):
                    tar.extractall(baseline_tree, filter="data")
                else:
                    # git archive produces a repository-controlled tree; older Python lacks the data filter.
                    for member in tar.getmembers():
                        target = (baseline_tree / member.name).resolve()
                        if baseline_tree.resolve() not in target.parents and target != baseline_tree.resolve():
                            raise RuntimeError(f"Unsafe path in git archive: {member.name}")
                    tar.extractall(baseline_tree)
            try:
                for project_name in ("SpyBrowser.Core", "SpyBrowser.Playwright"):
                    run(["dotnet", "pack", str(baseline_tree / "src" / project_name / (project_name + ".csproj")),
                         "-c", "Release", "-o", str(previous_feed), "-p:Version=" + args.previous_version,
                         "-p:MicrosoftPlaywrightVersion=" + PLAYWRIGHT], cwd=baseline_tree, env=os.environ.copy(), log=logs)
                previous_feed = previous_feed.resolve()
                statuses.append({"check": "previous-package-build", "status": "PASS", "baseline": BASELINE, "version": args.previous_version})
            except Exception as exc:
                statuses.append({"check": "previous-package-build", "status": "FAIL", "baseline": BASELINE, "reason": str(exc)})
                failed = {"schemaVersion": 1, "candidateVersion": args.candidate_version, "previousVersion": args.previous_version,
                          "baselineCommit": BASELINE, "candidateFeed": str(feed), "previousFeed": str(previous_feed),
                          "statuses": statuses, "technicalAllPassed": False,
                          "technicalFailures": [s for s in statuses if s["status"] == "FAIL"],
                          "technicalPending": [s for s in statuses if s["status"] == "PENDING"],
                          "externalPublicationAllowed": False, "allPassed": False, "commands": logs}
                evidence_path = output / "distribution-evidence.json"
                evidence_path.write_text(json.dumps(failed, indent=2) + "\n", encoding="utf-8")
                print(f"Evidence: {evidence_path}", file=sys.stderr)
                return 1
        else:
            if args.previous_feed_manifest:
                try:
                    baseline_manifest = verify_previous_feed(previous_feed, args.previous_feed_manifest.resolve(), args.repository.resolve(), args.previous_version)
                except RuntimeError as exc:
                    ap.error(str(exc))
                statuses.append({"check": "previous-package-provenance", "status": "PASS", "baseline": BASELINE,
                                 "version": args.previous_version, "sourceArchiveSha256": baseline_manifest["sourceArchiveSha256"]})
            else:
                statuses.append({"check": "previous-package-provenance", "status": "PENDING", "reason": "Caller-supplied previous feed; baseline provenance manifest was not independently verified."})

        # The only project source and execution directory are outside the product workspace.
        consumer = temp / "consumer"
        shutil.copytree(Path(__file__).parent, consumer, ignore=shutil.ignore_patterns("run.py", "bin", "obj"))
        if "<ProjectReference" in (consumer / "DistributionChecks.csproj").read_text(encoding="utf-8"):
            raise RuntimeError("Installed distribution consumer must not use ProjectReference.")
        with zipfile.ZipFile(feed / f"SpyBrowser.Playwright.{args.candidate_version}.nupkg") as package:
            nuspec_name = next(n for n in package.namelist() if n.endswith(".nuspec"))
            nuspec = ET.fromstring(package.read(nuspec_name))
            forbidden = ("SpyBrowser.Compatibility.CloakBrowser", "SpyBrowser.Cli")
            ids = [node.attrib.get("id", "") for node in nuspec.iter() if node.tag.endswith("dependency")]
            if any(dep in ids for dep in forbidden):
                raise RuntimeError(f"Playwright consumer package has forbidden dependencies: {ids}")
        sdk_version = subprocess.run(["dotnet", "--version"], cwd=Path.cwd(), check=True, capture_output=True, text=True).stdout.strip()
        (consumer / "global.json").write_text(json.dumps({"sdk": {"version": sdk_version, "rollForward": "latestPatch"}}, indent=2) + "\n", encoding="utf-8")
        dep_feeds = [Path(p).resolve() for p in args.dependency_feed]
        sources = [feed, previous_feed, *dep_feeds]
        source_xml = "".join(f"<add key={quoteattr(f'local-{i}')} value={quoteattr(p.as_posix())}/>" for i, p in enumerate(sources))
        (consumer / "NuGet.Config").write_text(
            "<?xml version=\"1.0\" encoding=\"utf-8\"?><configuration><packageSources><clear/>" +
            source_xml + "</packageSources></configuration>\n", encoding="utf-8")
        profile = temp / "shared-profile"  # same clean consumer profile across package switches; local filesystem semantics
        identity_dir = profile / "identity-store/identities/distribution-verification"
        identity_dir.mkdir(parents=True, exist_ok=True)
        project = consumer / "DistributionChecks.csproj"
        env = os.environ.copy()
        env["NUGET_PACKAGES"] = str(temp / "clean-nuget-cache")
        env["DOTNET_CLI_TELEMETRY_OPTOUT"] = "1"
        env["DOTNET_NOLOGO"] = "1"
        def phase(version, name):
            p_env = env.copy(); p_env["SPYBROWSER_PHASE"] = name
            props = ["-p:SpyBrowserVersion=" + version, "-p:MicrosoftPlaywrightVersion=" + PLAYWRIGHT]
            run(["dotnet", "restore", str(project), "--configfile", str(consumer / "NuGet.Config"), *props],
                cwd=consumer, env=p_env, log=logs)
            run(["dotnet", "build", str(project), "-c", "Release", "--no-restore", *props],
                cwd=consumer, env=p_env, log=logs)
            dll = consumer / "bin/Release/net8.0/DistributionChecks.dll"
            return run(["dotnet", str(dll), "--phase", name, "--profile-root", str(profile)],
                       cwd=temp, env=p_env, log=logs)

        state_path = profile / "storage-state.json"
        execution_error = None
        try:
            # Install and run the actual CLI tool package from the candidate feed alone; no source/project reference or network feed.
            cli_tool_dir = temp / "cli-tools"
            cli_env = env.copy(); cli_env["NUGET_PACKAGES"] = str(temp / "cli-nuget-cache")
            if os.name != "nt" and shutil.which("dotnet", path=cli_env.get("PATH")):
                cli_env["DOTNET_ROOT"] = str(Path(shutil.which("dotnet", path=cli_env.get("PATH"))).resolve().parent)
            try:
                run(["dotnet", "tool", "install", "SpyBrowser.Cli", "--tool-path", str(cli_tool_dir),
                     "--version", args.candidate_version, "--add-source", str(feed), "--ignore-failed-sources"],
                    cwd=consumer, env=cli_env, log=logs)
                cli_command = cli_tool_dir / ("spybrowser.exe" if os.name == "nt" else "spybrowser")
                cli_output = run([str(cli_command), "version"], cwd=consumer, env=cli_env, log=logs)
                if args.candidate_version not in cli_output:
                    raise RuntimeError(f"Installed CLI did not report candidate version {args.candidate_version}: {cli_output}")
                statuses.append({"check": "installed-cli-package-offline-version", "status": "PASS"})
            except Exception as exc:
                statuses.append({"check": "installed-cli-package-offline-version", "status": "FAIL", "reason": str(exc)})
            # Restore/build each version from packages only, using a fresh consumer cache and out-of-tree working directory.
            last_candidate_output = ""
            for version, name in [(args.candidate_version, "candidate-cursory"),
                                  (args.candidate_version, "candidate-bezier"),
                                  (args.candidate_version, "candidate-off")]:
                last_candidate_output = phase(version, name)
                statuses.append({"check": name, "status": "PASS"})
            if "PASS: installed snapshot round-trip" in last_candidate_output:
                statuses.append({"check": "snapshot-save-explicit-baseline-compare-concurrency-schema-secrets-discard", "status": "PASS"})
                if "PASS: OS-terminated SDK SaveAsync during actual temporary-file write" in last_candidate_output:
                    statuses.append({"check": "snapshot-os-crash-during-sdk-write-baseline-preserved", "status": "PASS"})
                else:
                    statuses.append({"check": "snapshot-os-crash-during-sdk-write-baseline-preserved", "status": "PENDING",
                                     "reason": "The bounded child process did not expose an active SDK temporary-file write; no fake writer evidence was accepted."})
                if "PASS: real Linux chmod" in last_candidate_output or "PASS: real Windows NTFS ACL denial" in last_candidate_output:
                    statuses.append({"check": "snapshot-real-permission-denial", "status": "PASS"})
                else:
                    statuses.append({"check": "snapshot-real-permission-denial", "status": "PENDING", "reason": "The host could not establish an enforced Linux chmod or Windows NTFS ACL denial; no fake file blocker was used."})
                if "PASS: snapshot-gpu-context-information field=webgl1.renderer-category severity=Information" in last_candidate_output:
                    statuses.append({"check": "snapshot-gpu-context-information", "status": "PASS"})
                else:
                    statuses.append({"check": "snapshot-gpu-context-information", "status": "FAIL",
                                     "reason": "Installed comparison did not emit the validated informational webgl1.renderer-category field change."})
            else:
                statuses.append({"check": "snapshot-save-compare-retention-permissions-cancellation-concurrency-schema-secrets", "status": "PENDING",
                                 "reason": "Snapshot API is not present in this candidate artifact; consumer runtime reflection did not find the approved public API."})
            # Preserve storage state checksum after candidate's last phase for rollback.
            expected_state_hash = sha(state_path) if state_path.exists() else None
            if expected_state_hash:
                env["EXPECTED_STORAGE_STATE_SHA256"] = expected_state_hash
            rollback_snapshot = profile / "snapshot-rollback-unsupported.json"
            if not rollback_snapshot.is_file():
                raise RuntimeError("Candidate did not preserve an unsupported newer snapshot fixture for rollback.")
            env["EXPECTED_ROLLBACK_SNAPSHOT_SHA256"] = sha(rollback_snapshot)
            rollback_output = phase(args.previous_version, "previous-bezier")
            statuses.append({"check": "previous-bezier-rollback", "status": "PASS"})
            if "PASS: previous package without snapshot API ignores newer snapshot unchanged" in rollback_output:
                statuses.append({"check": "previous-package-ignores-newer-snapshot", "status": "PASS",
                                 "reason": "The real pre-snapshot package opened the unchanged identity/profile/storage state while leaving a schema-999 optional snapshot byte-exact; no nonexistent reader compatibility is claimed."})
            else:
                statuses.append({"check": "previous-package-ignores-newer-snapshot", "status": "FAIL",
                                 "reason": "Older package did not confirm the required ignore/reject contract with a preserved newer-schema fixture."})
            phase(args.previous_version, "previous-off")
            statuses.append({"check": "previous-humanize-off", "status": "PASS"})
            statuses.append({"check": "browser-executable-and-official-driver", "status": "PASS",
                             "reason": "The installed-package launch completed through standard Microsoft.Playwright discovery."})
        except Exception as exc:
            execution_error = str(exc)
            statuses.append({"check": "consumer-execution", "status": "FAIL", "reason": execution_error})
        statuses.append({"check": "external-dataset-distribution-license", "status": "BLOCKED",
                         "reason": "Dataset license/redistribution clearance is not accepted; no external distribution is authorized."})
        technical_statuses = [s for s in statuses if s["status"] != "BLOCKED"]
        technical_all_passed = all(s["status"] == "PASS" for s in technical_statuses)
        manifest = {"schemaVersion": 1, "candidateVersion": args.candidate_version,
                    "sourceCommit": args.expected_source_commit, "declaredFinalCommit": args.declared_final_commit,
                    "isFinal": is_final, "candidateProvenanceStatus": "final" if is_final else "preliminary-not-release-evidence",
                    "feedManifest": str(manifest_path), "feedManifestSha256": sha(manifest_path),
                    "previousVersion": args.previous_version,
                    "baselineCommit": BASELINE if args.repository else None, "playwrightVersion": PLAYWRIGHT,
                    "candidateFeed": str(feed), "previousFeed": str(previous_feed), "dependencyFeeds": [str(p) for p in dep_feeds], "consumerWorkingDirectory": str(consumer),
                    "profileDirectory": str(profile), "manifestSha256": sha(identity_dir / "identity.json") if (identity_dir / "identity.json").exists() else None,
                    "storageStateSha256": sha(state_path) if state_path.exists() else None,
                    "packages": package_manifest([feed, previous_feed]),
                    "evidenceInputs": [{"path": str(Path(p).resolve()), "exists": Path(p).exists()} for p in args.evidence_input],
                    "statuses": statuses, "technicalAllPassed": technical_all_passed,
                    "technicalFailures": [s for s in technical_statuses if s["status"] == "FAIL"],
                    "technicalPending": [s for s in technical_statuses if s["status"] == "PENDING"],
                    "externalPublicationAllowed": False,
                    "allPassed": all(s["status"] == "PASS" for s in statuses),
                    "commands": logs}
        evidence_path = output / "distribution-evidence.json"
        evidence_path.write_text(json.dumps(manifest, indent=2) + "\n", encoding="utf-8")
        print(json.dumps({k: manifest[k] for k in ("candidateVersion", "previousVersion", "statuses", "allPassed")}, indent=2))
        print(f"Evidence: {evidence_path}")
        return 1 if execution_error else (0 if all(s["status"] == "PASS" for s in statuses) else 2)

if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except Exception as exc:
        print(f"FAIL: {exc}", file=sys.stderr)
        raise SystemExit(1)
