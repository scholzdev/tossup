#!/usr/bin/env python3
"""Package a built Unity player, preserving executable permissions and symlinks."""
import argparse
import os
from pathlib import Path
import shutil
import subprocess
import tarfile

ROOT = Path(__file__).resolve().parents[1]


def package(platform):
    out = ROOT / "dist" / platform
    out.mkdir(parents=True, exist_ok=True)
    if platform == "macos":
        app = ROOT / "Builds/macOS/Tossup.app"
        if not app.is_dir():
            raise RuntimeError(f"Missing player: {app}")
        subprocess.run(["ditto", "-c", "-k", "--sequesterRsrc", "--keepParent", str(app), str(out / "Tossup-macos.zip")], check=True)
    elif platform == "windows":
        source = ROOT / "Builds/Windows"
        if not (source / "Tossup.exe").is_file():
            raise RuntimeError("Missing Windows player")
        shutil.make_archive(str(out / "Tossup-windows"), "zip", source)
    else:
        source = ROOT / "Builds/Linux"
        if not (source / "Tossup.x86_64").is_file():
            raise RuntimeError("Missing Linux player")
        # A tarball is portable even when cross-building from macOS or Windows.
        with tarfile.open(out / "Tossup-linux.tar.gz", "w:gz") as archive:
            archive.add(source, arcname="Tossup")
        appimage_tool = os.environ.get("APPIMAGETOOL") or shutil.which("appimagetool")
        if not appimage_tool:
            raise RuntimeError("Linux tarball packaged; install appimagetool (or set APPIMAGETOOL) on Linux to also build Tossup.AppImage")
        appdir = out / "Tossup.AppDir"
        if appdir.exists():
            shutil.rmtree(appdir)
        appdir.mkdir()
        shutil.copytree(source, appdir / "usr/bin")
        shutil.copy(ROOT / "Assets/Resources/ui/icon.png", appdir / "tossup.png")
        (appdir / "tossup.desktop").write_text("[Desktop Entry]\nType=Application\nName=Tossup\nExec=Tossup.x86_64\nIcon=tossup\nCategories=Game;\n")
        launcher = appdir / "AppRun"
        launcher.write_text('#!/bin/sh\nHERE=$(CDPATH= cd "$(dirname "$0")" && pwd)\nexec "$HERE/usr/bin/Tossup.x86_64" "$@"\n')
        launcher.chmod(0o755)
        subprocess.run([appimage_tool, str(appdir), str(out / "Tossup.AppImage")], env={**os.environ, "ARCH": "x86_64"}, check=True)
    print(f"packaged {out}")


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("platform", choices=["macos", "windows", "linux"])
    try:
        package(parser.parse_args().platform)
    except (OSError, RuntimeError, subprocess.CalledProcessError) as error:
        parser.exit(1, f"{error}\n")
