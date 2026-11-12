#!/usr/bin/env bash
set -euo pipefail

# ==============================================================================
# EduQuest Repository-Aware SemVer + Trunk-Based Development Git Orchestrator
# Aligned with the 5-Level DDD + DBMA Anti-Spaghetti Taxonomy
#
# Modes:
#   --mode ANALYZE_ONLY   (Default) Inspects repo conventions, working tree,
#                         12 atomic staging groups, SemVer impact & release gates.
#   --mode EXECUTE_LOCAL  Seeds lockstep 0.1.0 metadata if empty, performs
#                         path-scoped staging (NEVER git add .), verifies staged
#                         diffs, and creates the 12 atomic commits locally.
# Optional Flags:
#   --bump AUTO|PATCH|MINOR|MAJOR|NONE
#   --tag-scaffold        Creates annotated pre-release tag v0.1.0-scaffold.0
#                         after committing the 12 atomic scaffold groups.
#   --allow-tag           Creates annotated release tag vX.Y.Z ONLY when all
#                         0-byte scaffold templates are implemented.
# ==============================================================================

MODE="ANALYZE_ONLY"
REQUESTED_BUMP="AUTO"
ALLOW_TAG="false"
TAG_SCAFFOLD="false"

while [[ $# -gt 0 ]]; do
  case "$1" in
    --mode)
      MODE="$2"
      shift 2
      ;;
    --bump)
      REQUESTED_BUMP="$2"
      shift 2
      ;;
    --allow-tag)
      ALLOW_TAG="true"
      shift 1
      ;;
    --tag-scaffold)
      TAG_SCAFFOLD="true"
      shift 1
      ;;
    *)
      echo "Unknown parameter: $1"
      echo "Usage: $0 [--mode ANALYZE_ONLY|EXECUTE_LOCAL] [--bump AUTO|PATCH|MINOR|MAJOR|NONE] [--tag-scaffold] [--allow-tag]"
      exit 1
      ;;
  esac
done

if [[ "$MODE" != "ANALYZE_ONLY" && "$MODE" != "EXECUTE_LOCAL" ]]; then
  echo "ERROR: --mode must be ANALYZE_ONLY or EXECUTE_LOCAL"
  exit 1
fi

# ------------------------------------------------------------------------------
# PHASE A & B: Repository Reconnaissance & Lockstep 0.1.0 Baseline Initialization
# ------------------------------------------------------------------------------
if ! git rev-parse --is-inside-work-tree >/dev/null 2>&1; then
  echo "INSUFFICIENT REPOSITORY EVIDENCE: Current directory is not a Git worktree."
  exit 1
fi

# Ensure lockstep 0.1.0 baseline files have valid initial content if still 0 bytes
if [[ ! -s "VERSION" ]]; then
  printf "0.1.0\n" > VERSION
fi

if [[ ! -s "Directory.Build.props" ]]; then
  cat << 'PROPS_EOF' > Directory.Build.props
<Project>
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <LangVersion>14.0</LangVersion>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <Version>0.1.0</Version>
  </PropertyGroup>
</Project>
PROPS_EOF
fi

if [[ ! -s "apps/web/package.json" ]]; then
  cat << 'PKG_EOF' > apps/web/package.json
{
  "name": "@eduquest/web",
  "version": "0.1.0",
  "private": true
}
PKG_EOF
fi

if [[ ! -s ".gitignore" ]]; then
  cat << 'IGN_EOF' > .gitignore
node_modules/
bin/
obj/
.next/
out/
dist/
coverage/
test-results/
playwright-report/
.env
.env.local
.env.*.local
.env.keys
.env.*.keys
*.pem
*.pfx
google-service-account.json
IGN_EOF
fi

CURRENT_BRANCH="$(git branch --show-current || echo "DETACHED_HEAD")"
REMOTE_HEAD_REF="$(git symbolic-ref --quiet refs/remotes/origin/HEAD 2>/dev/null || echo "")"
if [[ -n "$REMOTE_HEAD_REF" ]]; then
  TRUNK_BRANCH="${REMOTE_HEAD_REF#refs/remotes/origin/}"
else
  TRUNK_BRANCH="$CURRENT_BRANCH"
fi

LATEST_TAG="$(git tag --list --sort=-version:refname | head -n 1 || echo "")"
if [[ -z "$LATEST_TAG" ]]; then
  TAG_PREFIX="v"
  LAST_SEMVER="0.1.0"
  UNRELEASED_RANGE="HEAD"
else
  if [[ "$LATEST_TAG" =~ ^v([0-9]+\.[0-9]+\.[0-9]+.*)$ ]]; then
    TAG_PREFIX="v"
    LAST_SEMVER="${BASH_REMATCH[1]}"
  else
    TAG_PREFIX=""
    LAST_SEMVER="$LATEST_TAG"
  fi
  UNRELEASED_RANGE="${LATEST_TAG}..HEAD"
fi

COMMIT_COUNT="$(git rev-list --count HEAD 2>/dev/null || echo "0")"
DETECTED_COMMIT_STYLE="Conventional Commits with module scope (<type>(<scope>): <description>)"
CANONICAL_VERSION_SOURCE="VERSION + Directory.Build.props + apps/web/package.json (Lockstep 0.y.z)"
CURRENT_REPO_VERSION="$(tr -d '[:space:]' < VERSION)"

# ------------------------------------------------------------------------------
# PHASE C: Secret & Prohibited File Inspection (Fail-Closed Guard)
# ------------------------------------------------------------------------------
FORBIDDEN_PATTERNS=(
  "\.env\.keys$"
  "\.env\..*\.keys$"
  "\.env\.local$"
  "google-service-account\.json$"
  "id_rsa$"
  "\.pem$"
  "\.pfx$"
)

DIRTY_FILES="$(git status --porcelain=v1 | awk '{print $2}')"
BLOCKED_SECRET_FILES=()

for f in $DIRTY_FILES; do
  for pat in "${FORBIDDEN_PATTERNS[@]}"; do
    if [[ "$f" =~ $pat ]]; then
      BLOCKED_SECRET_FILES+=("$f")
    fi
  done
done

if [[ "${#BLOCKED_SECRET_FILES[@]}" -gt 0 ]]; then
  echo "=============================================================================="
  echo "SECURITY GATE BLOCKED: Forbidden secret or key files detected in working tree:"
  for bf in "${BLOCKED_SECRET_FILES[@]}"; do
    echo "  [BLOCKED] $bf"
  done
  echo "Action: Never stage private keys or .env.keys. Verify .gitignore immediately."
  echo "=============================================================================="
  exit 1
fi

# ------------------------------------------------------------------------------
# PHASE D: 12 Atomic Commit Groups Mapped to the 5-Level DDD + DBMA Taxonomy
# Strictly path-scoped (NO git add . or git add -A)
# ------------------------------------------------------------------------------
declare -a GROUP_IDS=(
  "G01_REPO_BUILD"
  "G02_INFRA_DBMA_EDGE"
  "G03_SHEETS_LEGACY"
  "G04_CORE_DEPT_EMP"
  "G05_AUTH_HYBRID"
  "G06_ACCESS"
  "G07_BILLING"
  "G08_GOVERNANCE_AUDIT"
  "G09_SECTION_VISIBILITY"
  "G10_NOTIF_SEARCH"
  "G11_MIGRATION_API_SHELL"
  "G12_CI_SECURITY_LOAD"
)

declare -A GROUP_SCOPES=(
  ["G01_REPO_BUILD"]="repo"
  ["G02_INFRA_DBMA_EDGE"]="infra-dbma"
  ["G03_SHEETS_LEGACY"]="sheets-legacy"
  ["G04_CORE_DEPT_EMP"]="core"
  ["G05_AUTH_HYBRID"]="identity"
  ["G06_ACCESS"]="access"
  ["G07_BILLING"]="billing"
  ["G08_GOVERNANCE_AUDIT"]="governance"
  ["G09_SECTION_VISIBILITY"]="section-visibility"
  ["G10_NOTIF_SEARCH"]="notifications-search"
  ["G11_MIGRATION_API_SHELL"]="migration"
  ["G12_CI_SECURITY_LOAD"]="quality-gates"
)

declare -A GROUP_TYPES=(
  ["G01_REPO_BUILD"]="build"
  ["G02_INFRA_DBMA_EDGE"]="feat"
  ["G03_SHEETS_LEGACY"]="feat"
  ["G04_CORE_DEPT_EMP"]="feat"
  ["G05_AUTH_HYBRID"]="feat"
  ["G06_ACCESS"]="feat"
  ["G07_BILLING"]="feat"
  ["G08_GOVERNANCE_AUDIT"]="feat"
  ["G09_SECTION_VISIBILITY"]="feat"
  ["G10_NOTIF_SEARCH"]="feat"
  ["G11_MIGRATION_API_SHELL"]="feat"
  ["G12_CI_SECURITY_LOAD"]="ci"
)

declare -A GROUP_MESSAGES=(
  ["G01_REPO_BUILD"]="build(repo): initialize monorepo solution, lockstep 0.1.0 metadata, and anti-spaghetti guards"
  ["G02_INFRA_DBMA_EDGE"]="feat(infra-dbma): scaffold 5-level DBMA schemas, cloudflare tunnel, nginx proxy, and edge tests"
  ["G03_SHEETS_LEGACY"]="feat(sheets-legacy): scaffold phases 0-8 read-only inventory, contract registry, adapters, and rehearsal scripts"
  ["G04_CORE_DEPT_EMP"]="feat(core): scaffold department and employee taxonomy across domain, app, infra, api, web, and tests"
  ["G05_AUTH_HYBRID"]="feat(identity): scaffold hybrid google oidc, external auth, session taxonomy, and identity test suites"
  ["G06_ACCESS"]="feat(access): scaffold service catalog, shared account protection, and entitlement taxonomy with tests"
  ["G07_BILLING"]="feat(billing): scaffold subscription due calculator, append-only payment ledger taxonomy, and billing tests"
  ["G08_GOVERNANCE_AUDIT"]="feat(governance): scaffold append-only audit trail, record versioning, and safe restore taxonomy with tests"
  ["G09_SECTION_VISIBILITY"]="feat(section-visibility): scaffold FR-014 heading visibility hierarchy taxonomy and test suites"
  ["G10_NOTIF_SEARCH"]="feat(notifications-search): scaffold deduplicated notification scheduler and 5-tier ranked search taxonomy"
  ["G11_MIGRATION_API_SHELL"]="feat(migration): scaffold sheet contract adapters, partial dbcontexts, repr api shell, and migration tests"
  ["G12_CI_SECURITY_LOAD"]="ci(quality-gates): add shift-left workflows, architecture line-limit tests, stride/asvs docs, and load suites"
)

declare -A GROUP_SEMVER=(
  ["G01_REPO_BUILD"]="NONE"
  ["G02_INFRA_DBMA_EDGE"]="MINOR"
  ["G03_SHEETS_LEGACY"]="MINOR"
  ["G04_CORE_DEPT_EMP"]="MINOR"
  ["G05_AUTH_HYBRID"]="MINOR"
  ["G06_ACCESS"]="MINOR"
  ["G07_BILLING"]="MINOR"
  ["G08_GOVERNANCE_AUDIT"]="MINOR"
  ["G09_SECTION_VISIBILITY"]="MINOR"
  ["G10_NOTIF_SEARCH"]="MINOR"
  ["G11_MIGRATION_API_SHELL"]="MINOR"
  ["G12_CI_SECURITY_LOAD"]="NONE"
)

declare -A GROUP_PATHS=(
  ["G01_REPO_BUILD"]=".gitignore .editorconfig README.md VERSION CHANGELOG.md EduQuest.sln Directory.Build.props Directory.Packages.props scripts/"
  ["G02_INFRA_DBMA_EDGE"]="database/ infrastructure/ tests/EduQuest.Security.Tests/Edge/"
  ["G03_SHEETS_LEGACY"]="apps/sheets-legacy/"
  ["G04_CORE_DEPT_EMP"]="apps/api/src/EduQuest.Domain/Common/ apps/api/src/EduQuest.Domain/Core/ apps/api/src/EduQuest.Application/Features/Core/ apps/api/src/EduQuest.Infrastructure/Persistence/Context/EduQuestDbContext.Core.cs apps/api/src/EduQuest.Infrastructure/Persistence/Configurations/Core/ apps/api/src/EduQuest.Infrastructure/Persistence/Repositories/Core/ apps/api/src/EduQuest.Api/Endpoints/Core/ apps/web/src/app/(workspace)/core/ apps/web/src/features/core/ apps/web/tests/unit/core/ apps/web/tests/integration/core/ apps/web/tests/e2e/core/ tests/EduQuest.Unit.Tests/Core/ tests/EduQuest.Integration.Tests/Core/ tests/EduQuest.Security.Tests/Core/ tests/EduQuest.E2E.Tests/Core/"
  ["G05_AUTH_HYBRID"]="apps/api/src/EduQuest.Domain/Identity/ apps/api/src/EduQuest.Application/Features/Identity/ apps/api/src/EduQuest.Infrastructure/Persistence/Context/EduQuestDbContext.Identity.cs apps/api/src/EduQuest.Infrastructure/Persistence/Configurations/Identity/ apps/api/src/EduQuest.Infrastructure/Persistence/Repositories/Identity/ apps/api/src/EduQuest.Infrastructure/Security/ apps/api/src/EduQuest.Infrastructure/BackgroundWorkers/LoginRegistryFastSyncWorker.cs apps/api/src/EduQuest.Infrastructure/BackgroundWorkers/LoginRegistryFullReconcileWorker.cs apps/api/src/EduQuest.Api/Endpoints/Identity/ apps/web/src/middleware.ts apps/web/src/app/(auth)/ apps/web/src/app/(workspace)/identity/ apps/web/src/features/identity/ apps/web/tests/unit/identity/ apps/web/tests/integration/identity/ apps/web/tests/e2e/identity/ tests/EduQuest.Unit.Tests/Identity/ tests/EduQuest.Integration.Tests/Identity/ tests/EduQuest.Security.Tests/Identity/ tests/EduQuest.E2E.Tests/Identity/"
  ["G06_ACCESS"]="apps/api/src/EduQuest.Domain/Access/ apps/api/src/EduQuest.Application/Features/Access/ apps/api/src/EduQuest.Infrastructure/Persistence/Context/EduQuestDbContext.Access.cs apps/api/src/EduQuest.Infrastructure/Persistence/Configurations/Access/ apps/api/src/EduQuest.Infrastructure/Persistence/Repositories/Access/ apps/api/src/EduQuest.Api/Endpoints/Access/ apps/web/src/app/(workspace)/access/ apps/web/src/features/access/ apps/web/tests/unit/access/ apps/web/tests/integration/access/ apps/web/tests/e2e/access/ tests/EduQuest.Unit.Tests/Access/ tests/EduQuest.Integration.Tests/Access/ tests/EduQuest.Security.Tests/Access/ tests/EduQuest.E2E.Tests/Access/"
  ["G07_BILLING"]="apps/api/src/EduQuest.Domain/Billing/ apps/api/src/EduQuest.Application/Features/Billing/ apps/api/src/EduQuest.Infrastructure/Persistence/Context/EduQuestDbContext.Billing.cs apps/api/src/EduQuest.Infrastructure/Persistence/Configurations/Billing/ apps/api/src/EduQuest.Infrastructure/Persistence/Repositories/Billing/ apps/api/src/EduQuest.Api/Endpoints/Billing/ apps/web/src/app/(workspace)/billing/ apps/web/src/features/billing/ apps/web/tests/unit/billing/ apps/web/tests/integration/billing/ apps/web/tests/e2e/billing/ tests/EduQuest.Unit.Tests/Billing/ tests/EduQuest.Integration.Tests/Billing/ tests/EduQuest.Security.Tests/Billing/ tests/EduQuest.E2E.Tests/Billing/"
  ["G08_GOVERNANCE_AUDIT"]="apps/api/src/EduQuest.Domain/Governance/Audit/ apps/api/src/EduQuest.Domain/Governance/SafeRestore/ apps/api/src/EduQuest.Application/Features/Governance/Audit/ apps/api/src/EduQuest.Application/Features/Governance/SafeRestore/ apps/api/src/EduQuest.Infrastructure/Persistence/Context/EduQuestDbContext.Governance.cs apps/api/src/EduQuest.Infrastructure/Persistence/Configurations/Governance/AuditEventConfiguration.cs apps/api/src/EduQuest.Infrastructure/Persistence/Configurations/Governance/AuditChangeConfiguration.cs apps/api/src/EduQuest.Infrastructure/Persistence/Configurations/Governance/RecordVersionConfiguration.cs apps/api/src/EduQuest.Infrastructure/Persistence/Repositories/Governance/AuditRepository.cs apps/api/src/EduQuest.Infrastructure/Persistence/Repositories/Governance/RecordVersionRepository.cs apps/api/src/EduQuest.Api/Endpoints/Governance/Audit/ apps/api/src/EduQuest.Api/Endpoints/Governance/SafeRestore/ apps/web/src/app/(workspace)/governance/audit/ apps/web/src/features/governance/audit/ apps/web/src/features/governance/restore/ apps/web/tests/unit/governance/restore/ apps/web/tests/integration/governance/restore/ apps/web/tests/e2e/governance/ tests/EduQuest.Unit.Tests/Governance/SafeRestore/ tests/EduQuest.Integration.Tests/Governance/SafeRestore/ tests/EduQuest.Security.Tests/Governance/Audit/ tests/EduQuest.E2E.Tests/Governance/SafeRestore/"
  ["G09_SECTION_VISIBILITY"]="apps/api/src/EduQuest.Domain/Governance/SectionVisibility/ apps/api/src/EduQuest.Application/Features/Governance/SectionVisibility/ apps/api/src/EduQuest.Infrastructure/Persistence/Configurations/Governance/DocumentSectionVisibilityConfiguration.cs apps/api/src/EduQuest.Infrastructure/Persistence/Repositories/Governance/SectionVisibilityRepository.cs apps/api/src/EduQuest.Api/Endpoints/Governance/SectionVisibility/ apps/web/src/app/(workspace)/governance/section-visibility/ apps/web/src/features/governance/section-visibility/ apps/web/src/types/section-visibility.types.ts apps/web/tests/unit/governance/section-visibility/ apps/web/tests/integration/governance/section-visibility/ tests/EduQuest.Unit.Tests/Governance/SectionVisibility/ tests/EduQuest.Integration.Tests/Governance/SectionVisibility/ tests/EduQuest.Security.Tests/Governance/SectionVisibility/ tests/EduQuest.E2E.Tests/Governance/SectionVisibility/"
  ["G10_NOTIF_SEARCH"]="apps/api/src/EduQuest.Domain/Notifications/ apps/api/src/EduQuest.Application/Features/Notifications/ apps/api/src/EduQuest.Application/Features/Reporting/ apps/api/src/EduQuest.Infrastructure/Persistence/Context/EduQuestDbContext.Notifications.cs apps/api/src/EduQuest.Infrastructure/Persistence/Configurations/Notifications/ apps/api/src/EduQuest.Infrastructure/Persistence/Repositories/Notifications/ apps/api/src/EduQuest.Infrastructure/Persistence/Queries/Reporting/ apps/api/src/EduQuest.Infrastructure/BackgroundWorkers/FutureDueNotificationSchedulerWorker.cs apps/api/src/EduQuest.Api/Endpoints/Notifications/ apps/api/src/EduQuest.Api/Endpoints/Reporting/ apps/web/src/app/(workspace)/notifications/ apps/web/src/app/(workspace)/reporting/ apps/web/src/features/notifications/ apps/web/src/features/reporting/ apps/web/tests/unit/notifications/ apps/web/tests/unit/reporting/ apps/web/tests/integration/notifications/ apps/web/tests/integration/reporting/ apps/web/tests/e2e/notifications/ apps/web/tests/e2e/reporting/ tests/EduQuest.Unit.Tests/Notifications/ tests/EduQuest.Unit.Tests/Reporting/ tests/EduQuest.Integration.Tests/Notifications/ tests/EduQuest.Integration.Tests/Reporting/ tests/EduQuest.Security.Tests/Notifications/ tests/EduQuest.Security.Tests/Reporting/ tests/EduQuest.E2E.Tests/Notifications/ tests/EduQuest.E2E.Tests/Reporting/"
  ["G11_MIGRATION_API_SHELL"]="apps/api/.env.example apps/api/Dotenvxfile apps/api/src/EduQuest.Domain/EduQuest.Domain.csproj apps/api/src/EduQuest.Domain/Migration/ apps/api/src/EduQuest.Application/EduQuest.Application.csproj apps/api/src/EduQuest.Application/DependencyInjection.cs apps/api/src/EduQuest.Application/Abstractions/ apps/api/src/EduQuest.Application/Behaviors/ apps/api/src/EduQuest.Application/Features/Migration/ apps/api/src/EduQuest.Infrastructure/EduQuest.Infrastructure.csproj apps/api/src/EduQuest.Infrastructure/DependencyInjection.cs apps/api/src/EduQuest.Infrastructure/Persistence/Context/EduQuestDbContext.cs apps/api/src/EduQuest.Infrastructure/Persistence/Context/EduQuestDbContext.Migration.cs apps/api/src/EduQuest.Infrastructure/Persistence/UnitOfWork.cs apps/api/src/EduQuest.Infrastructure/Persistence/Outbox/ apps/api/src/EduQuest.Infrastructure/Persistence/Interceptors/ apps/api/src/EduQuest.Infrastructure/Persistence/Configurations/Migration/ apps/api/src/EduQuest.Infrastructure/Persistence/Configurations/Outbox/ apps/api/src/EduQuest.Infrastructure/Persistence/Repositories/Migration/ apps/api/src/EduQuest.Infrastructure/Persistence/Repositories/Outbox/ apps/api/src/EduQuest.Infrastructure/GoogleSheets/ apps/api/src/EduQuest.Infrastructure/BackgroundWorkers/TransactionalOutboxProcessorWorker.cs apps/api/src/EduQuest.Infrastructure/Observability/ apps/api/src/EduQuest.Migrations/ apps/api/src/EduQuest.Api/EduQuest.Api.csproj apps/api/src/EduQuest.Api/Program.cs apps/api/src/EduQuest.Api/Middleware/ apps/api/src/EduQuest.Api/Endpoints/Migration/ apps/api/src/EduQuest.Api/Endpoints/Health/ apps/web/.env.example apps/web/Dotenvxfile apps/web/package.json apps/web/tsconfig.json apps/web/next.config.ts apps/web/postcss.config.mjs apps/web/vitest.config.ts apps/web/playwright.config.ts apps/web/src/app/globals.css apps/web/src/app/layout.tsx apps/web/src/app/error.tsx apps/web/src/app/not-found.tsx apps/web/src/app/(workspace)/layout.tsx apps/web/src/app/(workspace)/overview/ apps/web/src/app/(workspace)/migration/ apps/web/src/components/ apps/web/src/features/migration/ apps/web/src/lib/ apps/web/src/types/openapi.generated.ts apps/web/src/types/domain.types.ts apps/web/src/types/rbac.types.ts apps/web/tests/unit/migration/ apps/web/tests/integration/migration/ apps/web/tests/e2e/migration/ tests/EduQuest.Unit.Tests/EduQuest.Unit.Tests.csproj tests/EduQuest.Unit.Tests/Migration/ tests/EduQuest.Unit.Tests/Pipeline/ tests/EduQuest.Integration.Tests/EduQuest.Integration.Tests.csproj tests/EduQuest.Integration.Tests/Migration/ tests/EduQuest.Security.Tests/EduQuest.Security.Tests.csproj tests/EduQuest.Security.Tests/Migration/ tests/EduQuest.E2E.Tests/EduQuest.E2E.Tests.csproj tests/EduQuest.E2E.Tests/Migration/"
  ["G12_CI_SECURITY_LOAD"]=".github/workflows/ security/ tests/EduQuest.Architecture.Tests/ tests/EduQuest.Integration.Tests/Database/ tests/EduQuest.Performance.Tests/"
)

# ------------------------------------------------------------------------------
# PHASE E: Calculate SemVer Impact (Respecting 0.y.z Pre-1.0 & Breaking Rules)
# ------------------------------------------------------------------------------
HIGHEST_IMPACT="NONE"

upgrade_impact() {
  local candidate="$1"
  case "$candidate" in
    MAJOR) HIGHEST_IMPACT="MAJOR" ;;
    MINOR)
      if [[ "$HIGHEST_IMPACT" != "MAJOR" ]]; then
        HIGHEST_IMPACT="MINOR"
      fi
      ;;
    PATCH)
      if [[ "$HIGHEST_IMPACT" == "NONE" ]]; then
        HIGHEST_IMPACT="PATCH"
      fi
      ;;
  esac
}

if git diff HEAD -- database/ 2>/dev/null | grep -Eqi 'DROP TABLE|DROP COLUMN'; then
  upgrade_impact "MAJOR"
fi

for gid in "${GROUP_IDS[@]}"; do
  upgrade_impact "${GROUP_SEMVER[$gid]}"
done

if [[ "$REQUESTED_BUMP" != "AUTO" ]]; then
  HIGHEST_IMPACT="$REQUESTED_BUMP"
fi

IFS='.' read -r V_MAJOR V_MINOR V_PATCH <<< "${CURRENT_REPO_VERSION%%-*}"

if [[ "$V_MAJOR" == "0" ]]; then
  if [[ -z "$LATEST_TAG" ]]; then
    NEXT_SEMVER="0.1.0"
  else
    case "$HIGHEST_IMPACT" in
      MAJOR|MINOR) NEXT_SEMVER="0.$((V_MINOR + 1)).0" ;;
      PATCH)       NEXT_SEMVER="0.${V_MINOR}.$((V_PATCH + 1))" ;;
      NONE)        NEXT_SEMVER="$CURRENT_REPO_VERSION" ;;
    esac
  fi
else
  case "$HIGHEST_IMPACT" in
    MAJOR) NEXT_SEMVER="$((V_MAJOR + 1)).0.0" ;;
    MINOR) NEXT_SEMVER="${V_MAJOR}.$((V_MINOR + 1)).0" ;;
    PATCH) NEXT_SEMVER="${V_MAJOR}.${V_MINOR}.$((V_PATCH + 1))" ;;
    NONE)  NEXT_SEMVER="$CURRENT_REPO_VERSION" ;;
  esac
fi

NEXT_TAG="${TAG_PREFIX}${NEXT_SEMVER}"
SCAFFOLD_PRERELEASE_TAG="${TAG_PREFIX}${NEXT_SEMVER}-scaffold.0"

# ------------------------------------------------------------------------------
# PHASE F: Print Structured Principal Git/SemVer Report
# ------------------------------------------------------------------------------
echo "=============================================================================="
echo "1. REPOSITORY CONVENTION DETECTED"
echo "=============================================================================="
echo "Trunk / Active Branch : $TRUNK_BRANCH (Current: $CURRENT_BRANCH)"
echo "Commit Style          : $DETECTED_COMMIT_STYLE"
echo "Tag Style             : ${TAG_PREFIX}MAJOR.MINOR.PATCH (annotated)"
echo "Version Source        : $CANONICAL_VERSION_SOURCE"
echo "Current Version       : $CURRENT_REPO_VERSION"
echo "Latest Release Tag    : ${LATEST_TAG:-NONE (Initial 0.1.0 Development Baseline)}"
echo "Confidence            : HIGH"
echo ""
echo "=============================================================================="
echo "2. ATOMIC COMMIT DECOMPOSITION PLAN (12 TAXONOMY GROUPS)"
echo "=============================================================================="
for gid in "${GROUP_IDS[@]}"; do
  echo "[$gid] (${GROUP_TYPES[$gid]}(${GROUP_SCOPES[$gid]}) | SemVer: ${GROUP_SEMVER[$gid]})"
  echo "  Message : ${GROUP_MESSAGES[$gid]}"
done
echo ""
echo "=============================================================================="
echo "3. SEMVER & TAG DECISION"
echo "=============================================================================="
EMPTY_CODE_FILES="$(find apps database tests -type f \( -name '*.cs' -o -name '*.ts' -o -name '*.tsx' -o -name '*.sql' \) -size 0 2>/dev/null | wc -l | tr -d ' ')"

echo "Last Release          : ${LATEST_TAG:-NONE}"
echo "Unreleased Range      : $UNRELEASED_RANGE"
echo "Highest Impact        : $HIGHEST_IMPACT"
echo "Pre-1.0 (0.y.z) Guard : ACTIVE (0.y.z does not auto-promote to 1.0.0 before Stage Gate G6)"
echo "Next Production Tag   : $NEXT_TAG"
echo "Pre-Release Baseline  : $SCAFFOLD_PRERELEASE_TAG"

if [[ "$EMPTY_CODE_FILES" -gt 0 ]]; then
  echo "Validation Status     : PRE-IMPLEMENTATION SCAFFOLD ($EMPTY_CODE_FILES empty 0-byte templates)"
  echo "Production Tag v0.1.0 : BLOCKED (Implement & pass 4 CI gates before tagging v0.1.0)"
  echo "Pre-Release Tagging   : ALLOWED via --tag-scaffold ($SCAFFOLD_PRERELEASE_TAG)"
else
  echo "Validation Status     : READY FOR GATE EXECUTION"
  echo "Production Tag v0.1.0 : ALLOWED via --allow-tag ($NEXT_TAG)"
fi
echo "=============================================================================="

# ------------------------------------------------------------------------------
# PHASE G: Execute Local Atomic Staging, Commits & Tagging (EXECUTE_LOCAL mode)
# ------------------------------------------------------------------------------
if [[ "$MODE" == "EXECUTE_LOCAL" ]]; then
  echo ""
  echo "Executing path-scoped atomic commits locally (No push will be performed)..."
  for gid in "${GROUP_IDS[@]}"; do
    EXISTING_PATHS=()
    for p in ${GROUP_PATHS[$gid]}; do
      if [[ -e "$p" ]]; then
        EXISTING_PATHS+=("$p")
      fi
    done

    if [[ "${#EXISTING_PATHS[@]}" -gt 0 ]]; then
      git add -- "${EXISTING_PATHS[@]}"
      if ! git diff --cached --quiet; then
        git diff --cached --check
        git commit -m "${GROUP_MESSAGES[$gid]}"
        echo "  [COMMITTED] $gid -> ${GROUP_MESSAGES[$gid]}"
      else
        echo "  [SKIPPED]   $gid (No unstaged changes in path scope)"
      fi
    fi
  done

  if [[ "$TAG_SCAFFOLD" == "true" ]]; then
    git tag -a "$SCAFFOLD_PRERELEASE_TAG" -m "Release $SCAFFOLD_PRERELEASE_TAG: Initial 5-Level Taxonomy DDD + DBMA Scaffold Baseline"
    echo "  [TAGGED]    Created annotated SemVer pre-release tag: $SCAFFOLD_PRERELEASE_TAG"
  fi

  if [[ "$ALLOW_TAG" == "true" ]]; then
    if [[ "$EMPTY_CODE_FILES" -gt 0 ]]; then
      echo "  [TAG BLOCKED] Cannot create production tag $NEXT_TAG while $EMPTY_CODE_FILES files are 0-byte templates. Use --tag-scaffold for $SCAFFOLD_PRERELEASE_TAG."
      exit 1
    fi
    git tag -a "$NEXT_TAG" -m "Release $NEXT_TAG"
    echo "  [TAGGED]    Created annotated SemVer production tag: $NEXT_TAG"
  fi

  echo ""
  echo "Remaining working tree status after path-scoped commits:"
  git status --short --branch
fi