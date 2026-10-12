#!/usr/bin/env bash
# Fork -> UOBot mirror, by mask. C# is stripped of comments except the documentation above a type.
set -euo pipefail

S=/o/ModernUOfork/ModernUO-fork
F=/o/ModernUOfork/ModernUO-fork
M=/o/ModernUOfork/UOBot

# 1. Projects/BotAIv2: everything but C#, by extension, without build output or caches
cd "$F/Projects/BotAIv2"
find . -type f \( -name '*.md' -o -name '*.py' -o -name '*.patch' -o -name '*.csproj' -o -name '*.json' \) \
    -not -path '*/obj/*' -not -path '*/bin/*' -not -path '*/__pycache__/*' -print0 |
while IFS= read -r -d '' f; do
    mkdir -p "$M/Projects/BotAIv2/$(dirname "$f")"
    cp "$f" "$M/Projects/BotAIv2/$f"
done

# 2. C#, stripped: the bots, and the tiered navigation they walk by, which lives in the engine's tree with its tests
ENGINE="Projects/UOContent/Engines/Pathing/Tiered Projects/UOContent.Tests/Tests/Engines/Pathing/Tiered"
python "$S/mirror-strip.py" strip "$F/Projects/BotAIv2" "$M/Projects/BotAIv2"
for d in $ENGINE; do
    python "$S/mirror-strip.py" strip "$F/$d" "$M/$d"
done

# 3. files in the mirror's trees that the fork no longer has
for d in Projects/BotAIv2 $ENGINE; do
    cd "$M/$d"
    find . -type f -print0 | while IFS= read -r -d '' f; do
        if [ ! -e "$F/$d/$f" ]; then
            echo "gone from the fork: $d/$f"
            git -C "$M" rm -q "$d/$f"
        fi
    done
done

# 4. configuration: bot-*.json only, never the observer's memory or the before-* snapshots
for f in "$F"/Distribution/Configuration/bot-*.json; do
    b=$(basename "$f")
    case "$b" in
        bot-debugger-memory.json|*-before-*) continue ;;
    esac
    cp "$f" "$M/Distribution/Configuration/$b"
done
# and the bot-*.json the fork no longer has (bot-auction.json went with the auction, build 385)
for f in "$M"/Distribution/Configuration/bot-*.json; do
    b=$(basename "$f")
    if [ ! -e "$F/Distribution/Configuration/$b" ]; then
        echo "gone from the fork: Distribution/Configuration/$b"
        git -C "$M" rm -q "Distribution/Configuration/$b"
    fi
done

# 5. the scripts at the root, and the README at the root as well as in its folder
for f in START_SHARD.bat shard-status.sh; do
    cp "$F/$f" "$M/$f"
done
# the launchers START_SHARD.bat replaced on 11.10.2026
for f in start-shard.ps1 start-shard.cmd start-shard-detached.ps1 run-shard.ps1 install-shard-autostart.ps1; do
    git -C "$M" rm -q --ignore-unmatch "$f"
done
cp "$F/Projects/BotAIv2/README.md" "$M/README.md"

# 6. the web dashboard's page (the map picture is drawn by the shard itself and is not carried)
mkdir -p "$M/Distribution/Data/bot-web"
cp "$F/Distribution/Data/bot-web/index.html" "$M/Distribution/Data/bot-web/index.html"

# 6. the research, which lives only in the mirror: uobot-docs/ in the fork is docs/ there, without Python's caches
if [ -d "$F/uobot-docs" ]; then
    cd "$F/uobot-docs"
    find . -type f -not -path '*/__pycache__/*' -print0 |
    while IFS= read -r -d '' f; do
        mkdir -p "$M/docs/$(dirname "$f")"
        cp "$f" "$M/docs/$f"
    done
    cd "$M/docs"
    find . -type f -print0 | while IFS= read -r -d '' f; do
        if [ ! -e "$F/uobot-docs/$f" ] || [[ "$f" == */__pycache__/* ]]; then
            echo "gone from the fork: docs/$f"
            git -C "$M" rm -q --cached --ignore-unmatch "docs/$f"
            rm -f "$f"
        fi
    done
fi

# 7. the setup scripts, which only make sense in the mirror: they clone ModernUO and copy the mirror into it
cp "$F/uobot-setup.sh" "$M/setup.sh"
cp "$F/uobot-setup.ps1" "$M/setup.ps1"

echo "synced"
