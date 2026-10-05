#!/usr/bin/env python3
"""Gate, build and publish Unity releases; --dry-run never changes remote state."""
import argparse
import json
import os
from pathlib import Path
import re
import shutil
import subprocess

ROOT = Path(__file__).resolve().parents[1]


def run(*args, capture=False, **kwargs):
    result = subprocess.run(args, cwd=ROOT, check=True, text=True, stdout=subprocess.PIPE if capture else None, **kwargs)
    return result.stdout.strip() if capture else None


def release(options):
    version_path = ROOT / "Assets/Resources/version.json"
    original = version_path.read_text()
    version = json.loads(original)
    number = options.version or version["number"]
    if not re.fullmatch(r"\d+\.\d+\.\d+", number):
        raise ValueError("version must be major.minor.patch")
    tag = "v" + number
    if not options.dry_run:
        if run("git", "status", "--porcelain", capture=True):
            raise ValueError("Commit or stash changes before publishing a release")
        branch = run("git", "branch", "--show-current", capture=True)
        if branch not in ("main", "unity-6"):
            raise ValueError("Release from main or unity-6")
        run("gh", "auth", "status")
        run("git", "fetch", "origin", branch, "--tags")
        if run("git", "rev-parse", "HEAD", capture=True) != run("git", "rev-parse", "origin/" + branch, capture=True):
            raise ValueError("Release branch differs from origin; push or pull it first")
        if subprocess.run(["git", "rev-parse", "--verify", "refs/tags/" + tag], cwd=ROOT, stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL).returncode == 0:
            raise ValueError(f"Tag {tag} already exists")
    version_committed = False
    try:
        # Restore an override if verification or confirmation fails before commit.
        if number != version["number"]:
            version["number"] = number
            version_path.write_text(json.dumps(version) + "\n")
        env = {**os.environ, "DOTNET_ROLL_FORWARD": "Major"}
        run("dotnet", "run", "--project", "Tools/uitest/UiTest.csproj", "-c", "Release", "--disable-build-servers", "-p:NuGetAudit=false", "--", "1000", "11", env=env)
        run("sh", "Tools/build_docs.sh", "--check", env=env)
        run("sh", "Tools/build_all.sh", env=env)
        out = ROOT / "dist/release" / number
        out.mkdir(parents=True, exist_ok=True)
        artifacts = []
        for platform, filename, suffix in [("macos", "Tossup-macos.zip", "macos.zip"), ("windows", "Tossup-windows.zip", "windows.zip")]:
            target = out / f"Tossup-{number}-{suffix}"
            shutil.copy2(ROOT / "dist" / platform / filename, target)
            artifacts.append(str(target))
        previous = subprocess.run(["git", "describe", "--tags", "--abbrev=0"], cwd=ROOT, text=True, stdout=subprocess.PIPE, stderr=subprocess.DEVNULL)
        history = run("git", "log", "--no-merges", "--pretty=- %s", (previous.stdout.strip() + "..HEAD") if previous.returncode == 0 else "HEAD", capture=True)
        notes = out / "notes.md"
        notes.write_text(f"Tossup {number}\n\nDownloads\n\n- Windows: unzip and run Tossup.exe.\n- macOS: unzip Tossup.app; the build is ad hoc signed and is not notarized.\n\nEducational use only; see LICENSE.\n\nChanges\n\n{history}\n")
        if options.dry_run:
            print(f"Dry run: artifacts and notes at {out}; nothing published")
            return
        if not options.yes and input(f"Publish {tag} with artifacts in {out}? [y/N] ").lower() != "y":
            raise ValueError("Release cancelled before committing, tagging or publishing")
        if number != json.loads(original)["number"]:
            run("git", "add", "Assets/Resources/version.json")
            run("git", "commit", "-m", "Version " + number)
            version_committed = True
        run("git", "push", "origin", branch)
        run("git", "tag", "-a", tag, "-m", "Tossup " + number)
        run("git", "push", "origin", tag)
        run("gh", "release", "create", tag, *artifacts, "--title", "Tossup " + number, "--notes-file", str(notes), "--verify-tag")
    finally:
        if options.dry_run or not version_committed:
            version_path.write_text(original)


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("version", nargs="?")
    parser.add_argument("--dry-run", action="store_true")
    parser.add_argument("--yes", action="store_true")
    try:
        release(parser.parse_args())
    except (OSError, ValueError, subprocess.CalledProcessError) as error:
        parser.exit(1, f"{error}\n")
