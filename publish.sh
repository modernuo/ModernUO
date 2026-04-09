#!/bin/bash
set -e

REPO_ROOT="$(cd "$(dirname "$0")" && pwd)"

# Determine platform for native binary
detect_platform() {
    local os arch
    case "$(uname -s)" in
        Darwin) os="osx" ;;
        *)      os="linux" ;;
    esac

    case "$(uname -m)" in
        aarch64|arm64) arch="arm64" ;;
        *)             arch="x64" ;;
    esac

    echo "${os}-${arch}"
}

PLATFORM="$(detect_platform)"
TOOL_BINARY="$REPO_ROOT/tools/build-tool"
STAMP_FILE="$REPO_ROOT/tools/.build-tool-commit"
BUILD_TOOL_PROJECT="$REPO_ROOT/Projects/BuildTool/BuildTool.csproj"

# Get the commit hash of the latest change to BuildTool source
get_buildtool_commit() {
    git -C "$REPO_ROOT" log -1 --format=%H -- Projects/BuildTool/ 2>/dev/null
}

# Try to download native binary from GitHub Release
download_build_tool() {
    local asset_name="build-tool-${PLATFORM}"
    local tools_dir="$REPO_ROOT/tools"

    echo -e "\033[34mDownloading build tool...\033[0m"

    # Get latest release asset URL
    local release_url="https://api.github.com/repos/modernuo/ModernUO/releases/tags/build-tool-latest"
    local release_json
    release_json=$(curl -fsSL --connect-timeout 10 -H "User-Agent: ModernUO-BuildTool" "$release_url" 2>/dev/null) || return 1

    local download_url
    download_url=$(echo "$release_json" | grep -o "\"browser_download_url\"[[:space:]]*:[[:space:]]*\"[^\"]*${asset_name}[^\"]*\"" | head -1 | grep -o 'https://[^"]*') || return 1

    if [ -z "$download_url" ]; then
        return 1
    fi

    mkdir -p "$tools_dir"
    curl -fsSL --connect-timeout 10 -o "$TOOL_BINARY" "$download_url" || return 1
    chmod +x "$TOOL_BINARY"

    local current_commit
    current_commit="$(get_buildtool_commit)"
    if [ -n "$current_commit" ]; then
        echo -n "$current_commit" > "$STAMP_FILE"
    fi

    return 0
}

has_dotnet() {
    command -v dotnet >/dev/null 2>&1
}

# CentOS/RHEL globalization workaround
if [ -f /etc/os-release ]; then
    . /etc/os-release
    case "$ID" in
        centos|rhel)
            export DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1
            ;;
    esac
fi

# Try native binary first (with staleness check)
if [ -x "$TOOL_BINARY" ]; then
    current_commit="$(get_buildtool_commit)"
    if [ -n "$current_commit" ] && [ -f "$STAMP_FILE" ]; then
        stored_commit="$(cat "$STAMP_FILE")"
        if [ "$current_commit" = "$stored_commit" ]; then
            exec "$TOOL_BINARY" "$@"
        else
            echo -e "\033[33mBuild tool source has changed, updating...\033[0m"
            rm -f "$TOOL_BINARY"
        fi
    elif [ -z "$current_commit" ]; then
        # Not in a git repo — use binary as-is
        exec "$TOOL_BINARY" "$@"
    else
        # Binary exists but no stamp file — re-download to establish tracking
        rm -f "$TOOL_BINARY"
    fi
fi

# Try to download native binary
if download_build_tool 2>/dev/null; then
    exec "$TOOL_BINARY" "$@"
fi

# Fall back to dotnet run
if has_dotnet; then
    if [ -f "$BUILD_TOOL_PROJECT" ]; then
        echo -e "\033[33mUsing dotnet run fallback...\033[0m"
        exec dotnet run --project "$BUILD_TOOL_PROJECT" -- "$@"
    else
        echo "Error: BuildTool project not found at: $BUILD_TOOL_PROJECT" >&2
        exit 1
    fi
fi

# Nothing works
echo ""
echo -e "\033[31mError: Could not run the build tool.\033[0m"
echo ""
echo -e "\033[33mThe .NET 10 SDK is required. Download it from:\033[0m"
echo -e "\033[36m  https://dotnet.microsoft.com/download/dotnet/10.0\033[0m"
echo ""
exit 1
