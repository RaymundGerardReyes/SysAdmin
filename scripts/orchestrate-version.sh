#!/usr/bin/env bash
# ==============================================================================
# PipelineSolution Semantic Versioning & Git Release Orchestrator
# ==============================================================================
# Pure POSIX implementation: zero runtime dependencies beyond bash, git, sed, grep.
# Manages independent, decoupled Semantic Versioning across:
#   1. backend  (Directory.Build.props, .NET modular monolith)
#   2. api      (src/Pipeline.LocalApi/version.json, OpenAPI specifications)
#   3. frontend (src/Pipeline.Presentation/package.json, buildInfo.ts)
# ==============================================================================
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "${SCRIPT_DIR}/.." && pwd)"
cd "${REPO_ROOT}"

MANIFEST_FILE="${REPO_ROOT}/version.manifest.json"

COLOR_CYAN='\033[0;36m'
COLOR_GREEN='\033[0;32m'
COLOR_YELLOW='\033[1;33m'
COLOR_RED='\033[0;31m'
COLOR_BLUE='\033[0;34m'
COLOR_BOLD='\033[1m'
COLOR_RESET='\033[0m'

log_info() { echo -e "${COLOR_CYAN}[INFO]${COLOR_RESET} $1"; }
log_success() { echo -e "${COLOR_GREEN}[SUCCESS]${COLOR_RESET} $1"; }
log_warn() { echo -e "${COLOR_YELLOW}[WARN]${COLOR_RESET} $1"; }
log_error() { echo -e "${COLOR_RED}[ERROR]${COLOR_RESET} $1"; }
log_header() {
    echo -e "\n${COLOR_BOLD}${COLOR_BLUE}==============================================================================${COLOR_RESET}"
    echo -e "${COLOR_BOLD}${COLOR_BLUE} $1${COLOR_RESET}"
    echo -e "${COLOR_BOLD}${COLOR_BLUE}==============================================================================${COLOR_RESET}\n"
}

get_manifest_version() {
    local component="$1"
    grep -A 3 "\"${component}\":" "${MANIFEST_FILE}" | grep '"version":' | head -n 1 | sed -E 's/.*"version": "([^"]+)".*/\1/' | tr -d '[:space:]'
}

get_manifest_prefix() {
    local component="$1"
    grep -A 4 "\"${component}\":" "${MANIFEST_FILE}" | grep '"tagPrefix":' | head -n 1 | sed -E 's/.*"tagPrefix": "([^"]+)".*/\1/' | tr -d '[:space:]'
}

calculate_bump() {
    local current="$1"
    local bump_type="$2"
    local major minor patch
    IFS='.' read -r major minor patch <<< "$current"

    case "$bump_type" in
        major)
            echo "$((major + 1)).0.0"
            ;;
        minor)
            echo "${major}.$((minor + 1)).0"
            ;;
        patch)
            echo "${major}.${minor}.$((patch + 1))"
            ;;
        *)
            log_error "Unknown bump type: $bump_type (must be major, minor, or patch)"
            exit 1
            ;;
    esac
}

cmd_status() {
    log_header "PipelineSolution Decoupled Semantic Versioning Status"
    printf "%-12s | %-10s | %-16s | %-16s | %-10s\n" "Component" "Version" "Tag Prefix" "Latest Git Tag" "Commits Ahead"
    printf "%s\n" "--------------------------------------------------------------------------------"

    for comp in backend api frontend; do
        local ver tag_prefix latest_tag ahead_count
        ver="$(get_manifest_version "$comp")"
        tag_prefix="$(get_manifest_prefix "$comp")"
        latest_tag="$(git tag -l "${tag_prefix}*" --sort=-v:refname 2>/dev/null | head -n 1 || true)"
        
        if [ -z "$latest_tag" ]; then
            latest_tag="[none]"
            ahead_count="-"
        else
            ahead_count="$(git rev-list "${latest_tag}..HEAD" --count 2>/dev/null || echo "0")"
        fi

        printf "%-12s | %-10s | %-16s | %-16s | %-10s\n" "$comp" "$ver" "$tag_prefix" "$latest_tag" "$ahead_count"
    done
    echo ""
}

cmd_bump() {
    local comp="${1:-}"
    local bump_type="${2:-patch}"
    local force="${3:-}"

    if [ -z "$comp" ] || [[ ! "$comp" =~ ^(backend|api|frontend)$ ]]; then
        log_error "Usage: $0 bump <backend|api|frontend> <patch|minor|major> [--force]"
        exit 1
    fi

    if [[ ! "$bump_type" =~ ^(patch|minor|major)$ ]]; then
        log_error "Invalid bump type '$bump_type'. Allowed: patch, minor, major"
        exit 1
    fi

    if [ "$force" != "--force" ]; then
        if [ -n "$(git status --porcelain 2>/dev/null)" ]; then
            log_error "Working directory has uncommitted changes. Commit or stash first, or pass --force."
            exit 1
        fi
    fi

    local current_ver new_ver tag_prefix new_tag
    current_ver="$(get_manifest_version "$comp")"
    new_ver="$(calculate_bump "$current_ver" "$bump_type")"
    tag_prefix="$(get_manifest_prefix "$comp")"
    new_tag="${tag_prefix}${new_ver}"

    log_info "Bumping $comp from $current_ver to $new_ver ($bump_type)..."

    # 1. Update version.manifest.json using sed
    sed -i.bak -E "/\"${comp}\":/,/\"version\":/ s/\"version\": \"[^\"]+\"/\"version\": \"${new_ver}\"/" "${MANIFEST_FILE}"
    rm -f "${MANIFEST_FILE}.bak"

    # 2. Update component specific files
    case "$comp" in
        backend)
            sed -i.bak -E "s/<BackendVersion Condition=.*>[^<]+<\/BackendVersion>/<BackendVersion Condition=\"'\$(BackendVersion)' == ''\">${new_ver}<\/BackendVersion>/" "${REPO_ROOT}/Directory.Build.props"
            rm -f "${REPO_ROOT}/Directory.Build.props.bak"
            git add "${MANIFEST_FILE}" "${REPO_ROOT}/Directory.Build.props"
            ;;
        api)
            sed -i.bak -E "s/\"version\": \"[^\"]+\"/\"version\": \"${new_ver}\"/" "${REPO_ROOT}/src/Pipeline.LocalApi/version.json"
            sed -i.bak -E "s/\"openApiSpecVersion\": \"[^\"]+\"/\"openApiSpecVersion\": \"${new_ver}\"/" "${REPO_ROOT}/src/Pipeline.LocalApi/version.json"
            rm -f "${REPO_ROOT}/src/Pipeline.LocalApi/version.json.bak"
            sed -i.bak -E "s/<ApiVersion Condition=.*>[^<]+<\/ApiVersion>/<ApiVersion Condition=\"'\$(ApiVersion)' == ''\">${new_ver}<\/ApiVersion>/" "${REPO_ROOT}/src/Pipeline.LocalApi/Pipeline.LocalApi.csproj"
            rm -f "${REPO_ROOT}/src/Pipeline.LocalApi/Pipeline.LocalApi.csproj.bak"
            git add "${MANIFEST_FILE}" "${REPO_ROOT}/src/Pipeline.LocalApi/version.json" "${REPO_ROOT}/src/Pipeline.LocalApi/Pipeline.LocalApi.csproj"
            ;;
        frontend)
            sed -i.bak -E "s/\"version\": \"[^\"]+\"/\"version\": \"${new_ver}\"/" "${REPO_ROOT}/src/Pipeline.Presentation/package.json"
            rm -f "${REPO_ROOT}/src/Pipeline.Presentation/package.json.bak"
            sed -i.bak -E "s/version: '[^']+'/version: '${new_ver}'/" "${REPO_ROOT}/src/Pipeline.Presentation/src/config/buildInfo.ts"
            rm -f "${REPO_ROOT}/src/Pipeline.Presentation/src/config/buildInfo.ts.bak"
            git add "${MANIFEST_FILE}" "${REPO_ROOT}/src/Pipeline.Presentation/package.json" "${REPO_ROOT}/src/Pipeline.Presentation/src/config/buildInfo.ts"
            ;;
    esac

    # 3. Create Conventional Commit
    git commit -m "chore(${comp}): release ${new_tag}"
    # 4. Create Annotated Tag
    git tag -a "${new_tag}" -m "Release ${new_tag} for ${comp}"

    log_success "Bumped and tagged: ${new_tag}"
    log_info "Push using: git push origin main && git push origin ${new_tag}"
}

cmd_detect() {
    log_header "Conventional Commit Bump Detection"
    for comp in backend api frontend; do
        local tag_prefix latest_tag range
        tag_prefix="$(get_manifest_prefix "$comp")"
        latest_tag="$(git tag -l "${tag_prefix}*" --sort=-v:refname 2>/dev/null | head -n 1 || true)"

        if [ -z "$latest_tag" ]; then
            range="HEAD"
            echo -e "${COLOR_BOLD}${comp}${COLOR_RESET} (no previous tag found):"
        else
            range="${latest_tag}..HEAD"
            echo -e "${COLOR_BOLD}${comp}${COLOR_RESET} (since ${latest_tag}):"
        fi

        local commit_msgs
        commit_msgs="$(git log "$range" --format="%s" 2>/dev/null || echo "")"

        local rec="none"
        if echo "$commit_msgs" | grep -Eq "BREAKING CHANGE|${comp}!:"; then
            rec="major"
        elif echo "$commit_msgs" | grep -Eq "feat\(${comp}\):"; then
            rec="minor"
        elif echo "$commit_msgs" | grep -Eq "fix\(${comp}\):|perf\(${comp}\):"; then
            rec="patch"
        fi

        echo "  Detected recommendation: ${COLOR_GREEN}${rec}${COLOR_RESET}"
    done
    echo ""
}

cmd_init_baseline() {
    log_header "Initializing Baseline Commit & Scoped Semantic Versioning Tags"
    
    # 1. Connect local branch to origin/main if unborn
    local current_head
    current_head="$(git rev-parse HEAD 2>/dev/null || echo "")"
    if [ -z "$current_head" ]; then
        if git rev-parse origin/main >/dev/null 2>&1; then
            log_info "Unborn branch detected. Reconciling with origin/main..."
            git reset --mixed origin/main
        fi
    fi

    # 2. Stage all files
    log_info "Staging all tracked and untracked files (enforcing .gitignore)..."
    git add .

    # 3. Audit for any accidental binaries or temp files
    local staged_bad
    staged_bad="$(git diff --cached --name-only | grep -E "(bin/|obj/|node_modules/|\.suo$|\.user$)" || echo "")"
    if [ -n "$staged_bad" ]; then
        log_error "Prohibited files staged:"
        echo "$staged_bad"
        exit 1
    fi

    # 4. Create baseline commit
    log_info "Creating baseline initial architecture commit..."
    git commit -m "feat(infra): initial architecture baseline with decoupled SemVer orchestration"

    # 5. Create scoped tags
    log_info "Creating initial scoped baseline tags..."
    for comp in backend api frontend; do
        local ver tag_prefix tag_name
        ver="$(get_manifest_version "$comp")"
        tag_prefix="$(get_manifest_prefix "$comp")"
        tag_name="${tag_prefix}${ver}"
        if ! git rev-parse "$tag_name" >/dev/null 2>&1; then
            git tag -a "$tag_name" -m "Initial baseline release for ${comp} v${ver}"
            log_success "Created tag: $tag_name"
        else
            log_warn "Tag $tag_name already exists, skipping."
        fi
    done

    # 6. Push to remote
    log_info "Pushing main branch and tags to origin..."
    git push -u origin main --tags
    log_success "Baseline successfully committed, tagged, and pushed to remote GitHub repository!"
}

# Entrypoint dispatcher
case "${1:-status}" in
    status)
        cmd_status
        ;;
    bump)
        shift
        cmd_bump "$@"
        ;;
    detect)
        cmd_detect
        ;;
    init-baseline)
        cmd_init_baseline
        ;;
    *)
        echo "Usage: $0 {status|bump <backend|api|frontend> <patch|minor|major> [--force]|detect|init-baseline}"
        exit 1
        ;;
esac

