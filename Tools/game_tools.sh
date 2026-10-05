#!/bin/sh
set -eu
cd "$(dirname "$0")/.."
export DOTNET_ROLL_FORWARD=Major
# Keep compiler/status output off stdout, which is the content/trace interface.
dotnet build Tools/game/GameTools.csproj -c Release --nologo -v:quiet --disable-build-servers -p:NuGetAudit=false >&2
exec dotnet Tools/game/bin/Release/net10.0/GameTools.dll "$@"
