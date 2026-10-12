#!/usr/bin/env bash
# Builds a ModernUO shard with the bots from this repository.
#
#   ./setup.sh [folder]
#
# folder: where ModernUO goes (default: a folder called ModernUO next to this repository). The script clones
# ModernUO at the commit the bots are built against, copies the bots and their configuration in, applies the engine
# patches, registers the two bot assemblies and builds everything. Run it again after pulling this repository: it
# copies the new files over, skips what is already done and rebuilds.
set -euo pipefail

HERE="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
TARGET="${1:-$(dirname "$HERE")/ModernUO}"
PIN="be3a085"
URL="${MODERNUO_URL:-https://github.com/modernuo/ModernUO.git}"

need() {
    command -v "$1" >/dev/null 2>&1 || { echo "setup: $1 is needed ($2)" >&2; exit 1; }
}

need git "https://git-scm.com"
need dotnet "the .NET 10 SDK, https://dotnet.microsoft.com/download"

# 1. ModernUO, at the commit the bots are built against
if [ ! -d "$TARGET/.git" ]; then
    echo "setup: cloning ModernUO into $TARGET"
    git clone --quiet "$URL" "$TARGET"
    git -C "$TARGET" checkout --quiet "$PIN"
fi

# 2. The bots and the configuration they run with
cp -R "$HERE/Projects/." "$TARGET/Projects/"
mkdir -p "$TARGET/Distribution"
cp -R "$HERE/Distribution/." "$TARGET/Distribution/"

# 3. The engine patches, each once
for p in "$TARGET"/Projects/BotAIv2/engine-patches/*.patch; do
    name="$(basename "$p")"
    if git -C "$TARGET" apply --reverse --check "$p" >/dev/null 2>&1; then
        echo "setup: $name is already applied"
    else
        git -C "$TARGET" apply "$p"
        echo "setup: applied $name"
    fi
done

# 4. The two bot assemblies, registered once: in the solution, and in the list the server loads
sln="$TARGET/ModernUO.slnx"
if ! grep -q 'Projects/BotAIv2/BotAIv2.csproj' "$sln"; then
    awk '{ print }
         index($0, "Projects/Application/Application.csproj") {
             print "  <Project Path=\"Projects/BotAIv2/BotAIv2.csproj\" />"
             print "  <Project Path=\"Projects/BotAIv2/mindedBots/BotMindAI.csproj\" />"
         }' "$sln" > "$sln.tmp" && mv "$sln.tmp" "$sln"
    echo "setup: registered the bots in ModernUO.slnx"
fi

asm="$TARGET/Distribution/Data/assemblies.json"
if ! grep -q 'BotAIv2.dll' "$asm"; then
    awk '{
             if (index($0, "\"UOContent.dll\"") && !done) {
                 comma = ($0 ~ /,[ \t]*$/) ? "," : ""
                 print "  \"UOContent.dll\","
                 print "  \"BotAIv2.dll\","
                 print "  \"BotMindAI.dll\"" comma
                 done = 1
             } else {
                 print
             }
         }' "$asm" > "$asm.tmp" && mv "$asm.tmp" "$asm"
    echo "setup: registered the bots in Distribution/Data/assemblies.json"
fi

# 5. Build
echo "setup: building (the first build takes a few minutes)"
dotnet build "$sln" -c Release --nologo -v quiet

cat <<EOF

setup: done. Start the shard once and answer its questions (the client files, the expansion: UOR, the owner):

    cd "$TARGET/Distribution"
    dotnet ModernUO.dll

On Windows, run that from PowerShell or cmd rather than Git Bash, which is not a real console.
EOF
