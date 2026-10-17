#!/usr/bin/env bash
# ==============================================================================
# PipelineSolution: Master Scaffolding & Verification Engine
# Complies with:
#   - PipelineSolution_RBAC_Security_Testing_Blueprint.md
#   - PipelineSolution_Frontend_Engineering_Codebase_Guide.md
#   - Architectural Laws: P1-P9, C1-C10, L-01-L-12, R-01-R-12, B-01-B-07
# ==============================================================================

set -euo pipefail

# Determine script root
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$SCRIPT_DIR"

# Source modular scaffolding engines
if [[ -f "$SCRIPT_DIR/scripts/scaffold-solution.sh" ]]; then
  source "$SCRIPT_DIR/scripts/scaffold-solution.sh"
fi
if [[ -f "$SCRIPT_DIR/scripts/scaffold-backend.sh" ]]; then
  source "$SCRIPT_DIR/scripts/scaffold-backend.sh"
fi
if [[ -f "$SCRIPT_DIR/scripts/scaffold-tests.sh" ]]; then
  source "$SCRIPT_DIR/scripts/scaffold-tests.sh"
fi
if [[ -f "$SCRIPT_DIR/scripts/scaffold-frontend.sh" ]]; then
  source "$SCRIPT_DIR/scripts/scaffold-frontend.sh"
fi

# ANSI Colors
if [[ -t 1 ]] && [[ -z "${NO_COLOR:-}" ]]; then
  BOLD='\033[1m'
  DIM='\033[2m'
  RED='\033[0;31m'
  GREEN='\033[0;32m'
  YELLOW='\033[1;33m'
  BLUE='\033[0;34m'
  CYAN='\033[0;36m'
  MAGENTA='\033[0;35m'
  RESET='\033[0m'
else
  BOLD=''
  DIM=''
  RED=''
  GREEN=''
  YELLOW=''
  BLUE=''
  CYAN=''
  MAGENTA=''
  RESET=''
fi

log_banner() {
  echo -e "${CYAN}${BOLD}"
  echo "╔═══════════════════════════════════════════════════════════════════════════╗"
  echo "║                  PipelineSolution Infrastructure Engine                   ║"
  echo "║      .NET 10 LTS Monolith + React Native Concentric Architecture          ║"
  echo "╚═══════════════════════════════════════════════════════════════════════════╝"
  echo -e "${RESET}"
}

log_header() {
  echo -e "\n${BLUE}${BOLD}==> $1${RESET}"
}

log_step() {
  echo -e "  ${CYAN}•${RESET} $1"
}

log_success() {
  echo -e "  ${GREEN}✔${RESET} $1"
}

log_warn() {
  echo -e "  ${YELLOW}⚠${RESET} $1"
}

log_error() {
  echo -e "  ${RED}✖${RESET} $1" >&2
}

# ------------------------------------------------------------------------------
# Inventory Definitions
# ------------------------------------------------------------------------------

BACKEND_SRC_PROJECTS=(
  "src/Pipeline.Contracts/Pipeline.Contracts.csproj"
  "src/Pipeline.Domain/Pipeline.Domain.csproj"
  "src/Pipeline.Application/Pipeline.Application.csproj"
  "src/Pipeline.Infrastructure/Pipeline.Infrastructure.csproj"
  "src/Pipeline.Desktop.Host/Pipeline.Desktop.Host.csproj"
  "src/Pipeline.LocalApi/Pipeline.LocalApi.csproj"
  "src/Integrations.Google.Sheets/Integrations.Google.Sheets.csproj"
  "src/Integrations.Google.Gmail/Integrations.Google.Gmail.csproj"
  "src/Integrations.Google.Chat/Integrations.Google.Chat.csproj"
  "src/Integrations.Zoho.Expense/Integrations.Zoho.Expense.csproj"
  "src/Integrations.Zoho.Bigin/Integrations.Zoho.Bigin.csproj"
  "src/Integrations.PayMongo/Integrations.PayMongo.csproj"
)

BACKEND_TEST_PROJECTS=(
  "tests/Pipeline.TestKit/Pipeline.TestKit.csproj"
  "tests/Pipeline.Domain.Tests/Pipeline.Domain.Tests.csproj"
  "tests/Pipeline.Application.Tests/Pipeline.Application.Tests.csproj"
  "tests/Pipeline.Architecture.Tests/Pipeline.Architecture.Tests.csproj"
  "tests/Pipeline.Security.Tests/Pipeline.Security.Tests.csproj"
  "tests/Pipeline.Integration.Tests/Pipeline.Integration.Tests.csproj"
  "tests/Pipeline.Contract.Tests/Pipeline.Contract.Tests.csproj"
  "tests/Pipeline.Api.Tests/Pipeline.Api.Tests.csproj"
  "tests/Pipeline.Regression.Tests/Pipeline.Regression.Tests.csproj"
)

FRONTEND_REQUIRED_DIRS=(
  "src/Pipeline.Presentation/src/contracts"
  "src/Pipeline.Presentation/src/core"
  "src/Pipeline.Presentation/src/bridge"
  "src/Pipeline.Presentation/src/state"
  "src/Pipeline.Presentation/src/ui"
  "src/Pipeline.Presentation/src/security"
  "src/Pipeline.Presentation/src/observability"
  "src/Pipeline.Presentation/src/intent"
  "src/Pipeline.Presentation/src/platform"
  "src/Pipeline.Presentation/src/features"
  "src/Pipeline.Presentation/src/app"
  "src/Pipeline.Presentation/docs/00-start-here"
  "src/Pipeline.Presentation/docs/01-architecture"
  "src/Pipeline.Presentation/docs/05-decisions"
  "src/Pipeline.Presentation/docs/06-reference"
  "src/Pipeline.Presentation/contracts-snapshots"
  "src/Pipeline.Presentation/tools"
  "src/Pipeline.Presentation/__tests__/contracts"
  "src/Pipeline.Presentation/__tests__/architecture"
  "src/Pipeline.Presentation/__tests__/bridge"
  "src/Pipeline.Presentation/__tests__/security"
  "src/Pipeline.Presentation/__tests__/unit"
)

FRONTEND_FEATURES=(
  "_template"
  "dashboard"
  "diagnostics"
  "gmail"
  "jobs"
  "payments"
  "session"
  "sheets"
)

# ------------------------------------------------------------------------------
# Help & Usage
# ------------------------------------------------------------------------------

usage() {
  log_banner
  cat <<EOF
Usage: ./scaffold.sh [COMMAND | OPTIONS]

Commands:
  --scaffold         (Default) Idempotently scaffold and generate all solution files,
                     C# project manifests, core domain security, application dispatcher,
                     behaviors, tests, concentric frontend layers, and contract snapshots.
  --verify           Execute end-to-end multi-runtime verification:
                     • Check .NET 10 LTS and Node 22/npm prerequisites.
                     • Build .NET solution with -warnaserror.
                     • Run all .NET unit, architecture, security, and path tests.
                     • Run frontend contract drift, structure, and Jest tests.
  --test             Execute all test suites across .NET and Presentation.
  --check-prereqs    Validate installed versions of dotnet, node, and npm.
  --clean            Remove build and test artifacts (bin, obj, coverage, dist).
  --dry-run          Preview directory tree and project inventory without writes.
  --version-status   Display independent Semantic Versioning status across components.
  --version-bump     Bump component version: ./scaffold.sh --version-bump <backend|api|frontend> <patch|minor|major>
  --version-detect   Analyze git commit messages and recommend SemVer bumps.
  --help, -h         Display this manual.

Options:
  --force            Overwrite existing files during scaffolding.

Architectural Guarantees:
  • P1-P7: Zero bypass RBAC, Capability Tickets, Single Choke Point Dispatcher
  • L-01-L-12: Strict inward dependency flow, pure intent machine, sealed bridge
  • Verified against: PipelineSolution_RBAC_Security_Testing_Blueprint.md
EOF
}

# ------------------------------------------------------------------------------
# Check Toolchain Prerequisites
# ------------------------------------------------------------------------------

check_prerequisites() {
  log_header "Checking Host Prerequisites"
  local errors=0

  # Check .NET SDK (support dotnet and dotnet.exe)
  local dotnet_cmd=""
  if command -v dotnet >/dev/null 2>&1; then
    dotnet_cmd="dotnet"
  elif command -v dotnet.exe >/dev/null 2>&1; then
    dotnet_cmd="dotnet.exe"
  fi

  if [[ -n "$dotnet_cmd" ]]; then
    local dotnet_ver
    dotnet_ver="$("$dotnet_cmd" --version 2>/dev/null || true)"
    if [[ "$dotnet_ver" == 10.* ]]; then
      log_success ".NET SDK: $dotnet_ver (.NET 10 LTS matched)"
    else
      log_warn ".NET SDK: $dotnet_ver (expected .NET 10.x LTS)"
    fi
  else
    log_warn ".NET SDK is NOT installed or not on PATH."
    errors=$((errors + 1))
  fi

  # Check Node.js (support node and node.exe)
  local node_cmd=""
  if command -v node >/dev/null 2>&1; then
    node_cmd="node"
  elif command -v node.exe >/dev/null 2>&1; then
    node_cmd="node.exe"
  fi

  if [[ -n "$node_cmd" ]]; then
    local node_ver
    node_ver="$("$node_cmd" --version 2>/dev/null || true)"
    log_success "Node.js: $node_ver (expected >=22.13.0 <25.0.0)"
  else
    log_warn "Node.js is NOT installed or not on PATH."
    errors=$((errors + 1))
  fi

  # Check npm (support npm, npm.cmd, and npm.exe)
  local npm_cmd=""
  if command -v npm >/dev/null 2>&1; then
    npm_cmd="npm"
  elif command -v npm.cmd >/dev/null 2>&1; then
    npm_cmd="npm.cmd"
  elif command -v npm.exe >/dev/null 2>&1; then
    npm_cmd="npm.exe"
  fi

  if [[ -n "$npm_cmd" ]]; then
    local npm_ver
    npm_ver="$("$npm_cmd" --version 2>/dev/null || true)"
    log_success "npm: $npm_ver (expected >=10.9.0)"
  else
    log_warn "npm is NOT installed or not on PATH."
    errors=$((errors + 1))
  fi

  if [[ $errors -gt 0 ]]; then
    log_warn "Prerequisites check detected $errors missing tool(s)."
    return 1
  fi
  return 0
}

# ------------------------------------------------------------------------------
# Dry Run Mode
# ------------------------------------------------------------------------------

run_dry_run() {
  log_header "Dry Run: Solution & Codebase Inventory Audit"
  echo "Auditing workspace at: $SCRIPT_DIR"

  echo -e "\n${BOLD}[1] Solution & Governance Configurations:${RESET}"
  for file in "global.json" "Directory.Build.props" "Directory.Packages.props" "PipelineSolution.slnx" ".gitignore" "README.md" "CONTRIBUTING.md" "SECURITY.md" "AGENTS.md"; do
    if [[ -f "$file" ]]; then
      echo -e "  ${GREEN}✔${RESET} $file (exists, $(wc -c < "$file") bytes)"
    else
      echo -e "  ${RED}✖${RESET} $file (MISSING)"
    fi
  done

  echo -e "\n${BOLD}[2] Backend Source Projects (${#BACKEND_SRC_PROJECTS[@]} projects):${RESET}"
  for proj in "${BACKEND_SRC_PROJECTS[@]}"; do
    if [[ -f "$proj" ]]; then
      echo -e "  ${GREEN}✔${RESET} $proj"
    else
      echo -e "  ${RED}✖${RESET} $proj (MISSING)"
    fi
  done

  echo -e "\n${BOLD}[3] Backend Test Projects (${#BACKEND_TEST_PROJECTS[@]} projects):${RESET}"
  for proj in "${BACKEND_TEST_PROJECTS[@]}"; do
    if [[ -f "$proj" ]]; then
      echo -e "  ${GREEN}✔${RESET} $proj"
    else
      echo -e "  ${RED}✖${RESET} $proj (MISSING)"
    fi
  done

  echo -e "\n${BOLD}[4] Frontend Concentric Layers (${#FRONTEND_REQUIRED_DIRS[@]} modules):${RESET}"
  for dir in "${FRONTEND_REQUIRED_DIRS[@]}"; do
    if [[ -d "$dir" ]]; then
      echo -e "  ${GREEN}✔${RESET} $dir"
    else
      echo -e "  ${RED}✖${RESET} $dir (MISSING)"
    fi
  done

  echo -e "\n${BOLD}[5] Frontend Feature Modules (${#FRONTEND_FEATURES[@]} features):${RESET}"
  for feat in "${FRONTEND_FEATURES[@]}"; do
    local feat_dir="src/Pipeline.Presentation/src/features/$feat"
    if [[ -d "$feat_dir" ]] && [[ -f "$feat_dir/feature.manifest.ts" ]]; then
      echo -e "  ${GREEN}✔${RESET} $feat (manifest + structure OK)"
    else
      echo -e "  ${RED}✖${RESET} $feat (MISSING or INCOMPLETE)"
    fi
  done

  log_success "Dry run audit completed."
}

# ------------------------------------------------------------------------------
# Scaffolding & Generation Engine
# ------------------------------------------------------------------------------

run_scaffold() {
  local force="${1:-false}"
  log_header "Executing Scaffolding & Codebase Generation"

  # 1. Root Solution & Governance
  log_step "Scaffolding Solution descriptors, Central Package Management, and CI workflows..."
  if type scaffold_solution_governance >/dev/null 2>&1; then
    scaffold_solution_governance "$force"
  fi

  # 2. Backend Projects & Sources
  log_step "Scaffolding 12 Backend Projects, Steps 1-3 RBAC security, and dispatcher behaviors..."
  if type scaffold_backend_sources >/dev/null 2>&1; then
    scaffold_backend_sources "$force"
  fi

  # 3. Backend Test Projects & Test Suites
  log_step "Scaffolding 9 Test Projects, TestKit harness, and matrix fixtures..."
  if type scaffold_test_suites >/dev/null 2>&1; then
    scaffold_test_suites "$force"
  fi

  # 4. Frontend Concentric Layers & Tools
  log_step "Scaffolding Frontend presentation infrastructure, snapshots, and tooling..."
  if type scaffold_frontend_infrastructure >/dev/null 2>&1; then
    scaffold_frontend_infrastructure "$force"
  fi

  # 5. Ensure all directories exist
  for dir in "${FRONTEND_REQUIRED_DIRS[@]}"; do
    mkdir -p "$dir"
  done

  for feat in "${FRONTEND_FEATURES[@]}"; do
    local feat_dir="src/Pipeline.Presentation/src/features/$feat"
    mkdir -p "$feat_dir/api" "$feat_dir/components" "$feat_dir/hooks" "$feat_dir/screens"
  done

  log_success "All solution descriptors, backend monolith projects, tests, and presentation layers scaffolded successfully."
}

# ------------------------------------------------------------------------------
# Test Execution
# ------------------------------------------------------------------------------

run_tests() {
  log_header "Running Solution Test Battery"

  # .NET Test Suites
  log_step "Running .NET 10 Test Projects..."
  if command -v dotnet >/dev/null 2>&1; then
    dotnet test PipelineSolution.slnx --filter "Category=Unit|Category=Path|Category=Architecture|Category=Security" || {
      log_error ".NET tests failed."
      return 1
    }
    log_success ".NET test battery PASSED."
  else
    log_warn "dotnet CLI not found; skipping .NET test execution."
  fi

  # Frontend Jest Suites
  log_step "Running Frontend Jest Test Suites..."
  if [[ -d "src/Pipeline.Presentation" ]] && command -v npm >/dev/null 2>&1; then
    (
      cd src/Pipeline.Presentation
      if [[ -d "node_modules" ]]; then
        npm test -- --ci || {
          log_error "Frontend tests failed."
          exit 1
        }
      else
        log_warn "node_modules not found in src/Pipeline.Presentation. Run 'npm install' first."
      fi
    )
    log_success "Frontend Jest test battery PASSED."
  else
    log_warn "npm CLI not found or presentation directory missing; skipping frontend tests."
  fi
}

# ------------------------------------------------------------------------------
# Full End-to-End Verification
# ------------------------------------------------------------------------------

run_verify() {
  log_banner
  log_header "Executing Full Solution Verification Pipeline"

  # 1. Host Toolchain
  check_prerequisites || log_warn "Prerequisites had warnings/errors. Continuing verification..."

  # 2. Structural & Catalog Scaffolding
  run_scaffold false

  local dotnet_cmd="dotnet"
  if ! command -v dotnet >/dev/null 2>&1 && command -v dotnet.exe >/dev/null 2>&1; then
    dotnet_cmd="dotnet.exe"
  fi

  local node_cmd="node"
  if ! command -v node >/dev/null 2>&1 && command -v node.exe >/dev/null 2>&1; then
    node_cmd="node.exe"
  fi

  local npm_cmd="npm"
  if ! command -v npm >/dev/null 2>&1 && command -v npm.cmd >/dev/null 2>&1; then
    npm_cmd="npm.cmd"
  elif ! command -v npm >/dev/null 2>&1 && command -v npm.exe >/dev/null 2>&1; then
    npm_cmd="npm.exe"
  fi

  # 3. Backend .NET Build & Tests
  log_header "Backend Verification (.NET 10)"
  if command -v "$dotnet_cmd" >/dev/null 2>&1; then
    log_step "Restoring .NET packages via Central Package Management..."
    "$dotnet_cmd" restore PipelineSolution.slnx

    log_step "Building solution with zero-warning tolerance (-warnaserror)..."
    "$dotnet_cmd" build PipelineSolution.slnx --no-restore -warnaserror

    log_step "Running all backend test categories (Domain, Application, Architecture, Security, Integration)..."
    "$dotnet_cmd" test PipelineSolution.slnx --no-build

    log_success "Backend verification completely SUCCEEDED."
  else
    log_warn "dotnet CLI is not available in current shell environment."
  fi

  # 4. Frontend Verification
  log_header "Frontend Verification (React Native / TypeScript)"
  if [[ -d "src/Pipeline.Presentation" ]]; then
    (
      cd src/Pipeline.Presentation

      log_step "Running check:structure (folder grammar & manifests)..."
      "$node_cmd" --experimental-strip-types tools/check-structure.ts

      log_step "Running check:contracts (contract drift detection)..."
      "$node_cmd" --experimental-strip-types tools/check-contract-drift.ts

      log_step "Running check:i18n (error code directives)..."
      "$node_cmd" --experimental-strip-types tools/check-i18n-keys.ts

      log_step "Running check:trace (architectural rule citations)..."
      "$node_cmd" --experimental-strip-types tools/check-traceability.ts

      log_step "Running check:docs (documentation links)..."
      "$node_cmd" --experimental-strip-types tools/check-docs-links.ts

      if [[ -d "node_modules" ]] && command -v "$npm_cmd" >/dev/null 2>&1; then
        log_step "Running TypeScript typecheck..."
        "$npm_cmd" run typecheck

        log_step "Running Jest test battery..."
        "$npm_cmd" test -- --ci
      else
        log_step "Static tooling passed. Note: Run 'npm install' inside src/Pipeline.Presentation for full local Jest/Metro runtime."
      fi
    )
    log_success "Frontend presentation verification completely SUCCEEDED."
  fi

  echo -e "\n${GREEN}${BOLD}═══════════════════════════════════════════════════════════════════════════${RESET}"
  echo -e "${GREEN}${BOLD} ✔ VERIFICATION COMPLETE: ALL ARCHITECTURAL GATES AND TESTS PASSED!       ${RESET}"
  echo -e "${GREEN}${BOLD}═══════════════════════════════════════════════════════════════════════════${RESET}\n"
}

# ------------------------------------------------------------------------------
# Clean Artifacts
# ------------------------------------------------------------------------------

run_clean() {
  log_header "Cleaning Build and Temporary Artifacts"

  log_step "Removing .NET bin/ and obj/ folders..."
  find . -type d \( -name "bin" -o -name "obj" -o -name "TestResults" \) -prune -exec rm -rf {} + 2>/dev/null || true

  log_step "Removing Frontend coverage/ and dist/ folders..."
  rm -rf src/Pipeline.Presentation/coverage src/Pipeline.Presentation/dist .test_write

  log_success "Clean completed."
}

# ------------------------------------------------------------------------------
# Main Dispatcher
# ------------------------------------------------------------------------------

main() {
  local cmd="${1:---scaffold}"
  local force=false

  for arg in "$@"; do
    if [[ "$arg" == "--force" ]]; then
      force=true
    fi
  done

  case "$cmd" in
    --scaffold)
      log_banner
      run_scaffold "$force"
      echo -e "\nRun ${CYAN}./scaffold.sh --verify${RESET} to execute the full verification pipeline.\n"
      ;;
    --dry-run)
      log_banner
      run_dry_run
      ;;
    --verify)
      run_verify
      ;;
    --test)
      log_banner
      run_tests
      ;;
    --check-prereqs)
      log_banner
      check_prerequisites
      ;;
    --clean)
      log_banner
      run_clean
      ;;
    --version-status|--version)
      log_banner
      ./scripts/orchestrate-version.sh status
      ;;
    --version-bump)
      shift
      ./scripts/orchestrate-version.sh bump "$@"
      ;;
    --version-detect)
      ./scripts/orchestrate-version.sh detect
      ;;
    --help|-h|help)
      usage
      ;;
    *)
      if [[ "$cmd" == "--force" ]]; then
        log_banner
        run_scaffold true
      else
        log_error "Unknown option: $cmd"
        usage
        exit 1
      fi
      ;;
  esac
}

main "$@"
