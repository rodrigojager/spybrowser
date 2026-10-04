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
    result = subprocess.run(command, cwd=cwd, env=env, text=True, stdout=subprocess.PIPE, stderr=subprocess.STDOUT)
    log.append({"command": [str(x) for x in command], "cwd": str(cwd), "exitCode": result.returncode, "output": result.stdout})
    if result.returncode:
        raise RuntimeError(f"Command failed ({result.returncode}): {' '.join(map(str, command))}\n{result.stdout}")
    return result.stdout


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
                             "runtimeAssemblies": {n: hashlib.sha256(z.read(n)).hexdigest() for n in names if n.endswith(".dll") and "/lib/" in n},
                             "licenseAndSourceEntries": {n: hashlib.sha256(z.read(n)).hexdigest() for n in names if "license" in n.lower() or n.startswith("source/") or "notice" in n.lower()},
                             "symbols": next(({"file": p.with_name(p.stem + ".snupkg").name,
                                                "sha256": sha(p.with_name(p.stem + ".snupkg"))}
                                               for _ in [0] if p.with_name(p.stem + ".snupkg").exists()), None)})
    return rows


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--feed", type=Path, required=True, help="Existing final local NuGet feed; never repacked from source")
    ap.add_argument("--candidate-version", required=True)
    ap.add_argument("--previous-feed", type=Path)
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
    logs = []
    statuses = []
    previous_feed = args.previous_feed.resolve() if args.previous_feed else None
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
            archive = subprocess.run(["git", "-c", f"safe.directory={args.repository.resolve().as_posix()}", "-C", str(args.repository.resolve()), "archive", BASELINE], stdout=subprocess.PIPE, stderr=subprocess.PIPE)
            if archive.returncode:
                ap.error(f"Cannot read baseline {BASELINE}: {archive.stderr.decode(errors='replace')}")
            import tarfile, io
            with tarfile.open(fileobj=io.BytesIO(archive.stdout), mode="r:") as tar:
                tar.extractall(baseline_tree, filter="data")
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
                          "statuses": statuses, "allPassed": False, "commands": logs}
                evidence_path = output / "distribution-evidence.json"
                evidence_path.write_text(json.dumps(failed, indent=2) + "\n", encoding="utf-8")
                print(f"Evidence: {evidence_path}", file=sys.stderr)
                return 1
        else:
            statuses.append({"check": "previous-package-provenance", "status": "PENDING", "reason": "Caller-supplied previous feed; baseline e217 provenance must be independently verified."})

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
        source_xml = "".join(f"<add key=\"local-{i}\" value=\"{p.as_posix()}\"/>" for i, p in enumerate(sources))
        (consumer / "NuGet.Config").write_text(
            "<?xml version=\"1.0\" encoding=\"utf-8\"?><configuration><packageSources><clear/>" +
            source_xml + "</packageSources></configuration>\n", encoding="utf-8")
        profile = output / "shared-profile"  # same manifest/profile directory across package switches
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
            # Restore/build each version from packages only, using a fresh consumer cache and out-of-tree working directory.
            last_candidate_output = ""
            for version, name in [(args.candidate_version, "candidate-cursory"),
                                  (args.candidate_version, "candidate-bezier"),
                                  (args.candidate_version, "candidate-off")]:
                last_candidate_output = phase(version, name)
                statuses.append({"check": name, "status": "PASS"})
            if "PASS: installed snapshot round-trip" in last_candidate_output:
                statuses.append({"check": "snapshot-save-explicit-baseline-compare-concurrency-schema-secrets-discard", "status": "PASS"})
                if "PASS: real Linux chmod" in last_candidate_output:
                    statuses.append({"check": "snapshot-real-permission-denial", "status": "PASS"})
                else:
                    statuses.append({"check": "snapshot-real-permission-denial", "status": "PENDING", "reason": "The host cannot establish real Linux chmod denial or is not Linux; no fake file blocker was used."})
                statuses.append({"check": "snapshot-interrupted-write-midstream-and-gpu-context-information", "status": "PENDING",
                                 "reason": "Only cancellation cleanup is tested; public fault injection is unavailable and GPU/context comparison needs the final diagnostic capture contract."})
            else:
                statuses.append({"check": "snapshot-save-compare-retention-permissions-cancellation-concurrency-schema-secrets", "status": "PENDING",
                                 "reason": "Snapshot API is not present in this candidate artifact; consumer runtime reflection did not find the approved public API."})
            # Preserve storage state checksum after candidate's last phase for rollback.
            expected_state_hash = sha(state_path) if state_path.exists() else None
            if expected_state_hash:
                env["EXPECTED_STORAGE_STATE_SHA256"] = expected_state_hash
            phase(args.previous_version, "previous-bezier")
            statuses.append({"check": "previous-bezier-rollback", "status": "PASS"})
            phase(args.previous_version, "previous-off")
            statuses.append({"check": "previous-humanize-off", "status": "PASS"})
            statuses.append({"check": "browser-executable-and-official-driver", "status": "PASS",
                             "reason": "The installed-package launch completed through standard Microsoft.Playwright discovery."})
        except Exception as exc:
            execution_error = str(exc)
            statuses.append({"check": "consumer-execution", "status": "FAIL", "reason": execution_error})
        statuses.append({"check": "external-dataset-distribution-license", "status": "BLOCKED",
                         "reason": "Dataset license/redistribution clearance is not accepted; no external distribution is authorized."})
        manifest = {"schemaVersion": 1, "candidateVersion": args.candidate_version, "previousVersion": args.previous_version,
                    "baselineCommit": BASELINE if args.repository else None, "playwrightVersion": PLAYWRIGHT,
                    "candidateFeed": str(feed), "previousFeed": str(previous_feed), "dependencyFeeds": [str(p) for p in dep_feeds], "consumerWorkingDirectory": str(consumer),
                    "profileDirectory": str(profile), "manifestSha256": sha(identity_dir / "identity.json") if (identity_dir / "identity.json").exists() else None,
                    "storageStateSha256": sha(state_path) if state_path.exists() else None,
                    "packages": package_manifest([feed, previous_feed]),
                    "evidenceInputs": [{"path": str(Path(p).resolve()), "exists": Path(p).exists()} for p in args.evidence_input],
                    "statuses": statuses, "allPassed": all(s["status"] == "PASS" for s in statuses),
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
