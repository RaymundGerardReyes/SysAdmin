#!/usr/bin/env bash
# ==============================================================================
# PipelineSolution Modular Scaffolder: Frontend Concentric Infrastructure & Tools
# ==============================================================================

set -euo pipefail

write_file() {
  local target="$1"
  local content="$2"
  local force="${3:-false}"

  if [[ -f "$target" ]] && [[ "$force" != "true" ]]; then
    return 0
  fi

  mkdir -p "$(dirname "$target")"
  printf '%s\n' "$content" > "$target"
}

scaffold_frontend_infrastructure() {
  local force="${1:-false}"
  local p_root="src/Pipeline.Presentation"

  # 1. package.json
  write_file "$p_root/package.json" '{
  "name": "@pipeline/presentation",
  "version": "1.0.0",
  "private": true,
  "description": "Pipeline Presentation: React Native (Windows + iOS) + Expo Modules + NativeWind v4",
  "engines": {
    "node": ">=22.13.0 <25.0.0",
    "npm": ">=10.9.0"
  },
  "packageManager": "npm@11.7.0",
  "scripts": {
    "typecheck": "tsc --noEmit",
    "lint": "eslint src __tests__ tools --max-warnings=0",
    "test": "jest",
    "test:ci": "jest --ci --coverage --maxWorkers=2",
    "check:structure": "ts-node tools/check-structure.ts",
    "check:contracts": "ts-node tools/check-contract-drift.ts",
    "check:i18n": "ts-node tools/check-i18n-keys.ts",
    "check:trace": "ts-node tools/check-traceability.ts",
    "check:docs": "ts-node tools/check-docs-links.ts",
    "docs:build": "ts-node tools/build-docs-reference.ts",
    "gen:feature": "ts-node tools/generate-feature/index.ts",
    "gen:command": "ts-node tools/generate-command/index.ts",
    "verify": "npm run check:structure && npm run check:contracts && npm run check:trace && npm run typecheck && npm run test"
  },
  "dependencies": {
    "@tanstack/react-query": "^5.66.0",
    "clsx": "^2.1.1",
    "expo": "~53.0.0",
    "nativewind": "^4.2.7",
    "react": "19.0.0",
    "react-native": "0.78.2",
    "react-native-windows": "0.78.2",
    "tailwind-merge": "^3.0.2",
    "zod": "^3.24.2",
    "zustand": "^5.0.3"
  },
  "devDependencies": {
    "@testing-library/react-native": "^13.1.0",
    "@types/jest": "^29.5.14",
    "@types/node": "^22.13.0",
    "@types/react": "^19.0.0",
    "dependency-cruiser": "^16.10.0",
    "eslint": "^9.21.0",
    "jest": "^29.7.0",
    "knip": "^5.44.0",
    "tailwindcss": "^3.4.17",
    "ts-jest": "^29.2.6",
    "ts-node": "^10.9.2",
    "typescript": "~5.8.2"
  }
}' "$force"

  # 2. tsconfig.json
  write_file "$p_root/tsconfig.json" '{
  "compilerOptions": {
    "target": "ES2022",
    "module": "commonjs",
    "lib": ["ES2022", "DOM"],
    "jsx": "react-jsx",
    "strict": true,
    "noUncheckedIndexedAccess": true,
    "exactOptionalPropertyTypes": true,
    "noImplicitOverride": true,
    "noImplicitReturns": true,
    "noFallthroughCasesInSwitch": true,
    "moduleResolution": "node",
    "esModuleInterop": true,
    "skipLibCheck": true,
    "forceConsistentCasingInFileNames": true,
    "baseUrl": "."
  },
  "include": ["src/**/*", "__tests__/**/*", "tools/**/*"],
  "exclude": ["node_modules", "dist", "coverage"]
}' "$force"

  # 3. jest.config.js
  write_file "$p_root/jest.config.js" 'module.exports = {
  preset: "ts-jest",
  testEnvironment: "node",
  testMatch: ["**/__tests__/**/*.test.ts", "**/__tests__/**/*.test.tsx"],
  moduleFileExtensions: ["ts", "tsx", "js", "jsx", "json"],
  transform: {
    "^.+\\.(ts|tsx)$": ["ts-jest", { tsconfig: "tsconfig.json" }]
  },
  coverageThreshold: {
    global: {
      branches: 80,
      functions: 80,
      lines: 80,
      statements: 80
    }
  }
};' "$force"

  # 4. Snapshots
  write_file "$p_root/contracts-snapshots/permissions.snapshot.json" '[
  "jobs.read",
  "jobs.cancel",
  "sheets.values.read",
  "sheets.values.write",
  "gmail.messages.send",
  "chat.messages.create",
  "zoho.expense.read",
  "zoho.expense.create",
  "bigin.contacts.read",
  "bigin.contacts.create",
  "payments.status.read",
  "payments.checkout.create",
  "payments.checkout.approve",
  "integrations.connect",
  "integrations.disconnect",
  "admin.users.manage",
  "admin.policy.update",
  "admin.audit.read"
]' "$force"

  write_file "$p_root/contracts-snapshots/errorCodes.snapshot.json" '[
  "AUTHN_MISSING",
  "SESSION_INVALID",
  "POLICY_INVALID",
  "OPERATION_UNMAPPED",
  "PERMISSION_MISSING",
  "RESOURCE_OUT_OF_SCOPE",
  "STEPUP_REQUIRED",
  "APPROVAL_REQUIRED",
  "SOD_VIOLATION",
  "RATE_LIMITED",
  "INTERNAL_DENY",
  "VALIDATION_FAILED",
  "IDEMPOTENCY_CONFLICT",
  "OPERATION_IN_PROGRESS",
  "CREDENTIAL_DENIED",
  "EGRESS_BLOCKED",
  "NOT_FOUND",
  "INVALID_REQUEST"
]' "$force"

  write_file "$p_root/contracts-snapshots/jobStatus.snapshot.json" '[
  "Pending",
  "Validating",
  "Running",
  "Failed",
  "Succeeded",
  "RetryScheduled",
  "NeedsReauthorization",
  "Cancelled"
]' "$force"

  write_file "$p_root/contracts-snapshots/commands.snapshot.json" '[
  { "command": "sheets.read", "permission": "sheets.values.read", "tier": "T0", "mutating": false },
  { "command": "sheets.write", "permission": "sheets.values.write", "tier": "T1", "mutating": true },
  { "command": "gmail.send", "permission": "gmail.messages.send", "tier": "T2", "mutating": true },
  { "command": "chat.create", "permission": "chat.messages.create", "tier": "T2", "mutating": true },
  { "command": "zoho.expense.read", "permission": "zoho.expense.read", "tier": "T0", "mutating": false },
  { "command": "zoho.expense.create", "permission": "zoho.expense.create", "tier": "T2", "mutating": true },
  { "command": "bigin.contacts.read", "permission": "bigin.contacts.read", "tier": "T0", "mutating": false },
  { "command": "bigin.contacts.create", "permission": "bigin.contacts.create", "tier": "T2", "mutating": true },
  { "command": "payments.status.read", "permission": "payments.status.read", "tier": "T0", "mutating": false },
  { "command": "payments.checkout.create", "permission": "payments.checkout.create", "tier": "T3", "mutating": true },
  { "command": "payments.checkout.approve", "permission": "payments.checkout.approve", "tier": "T3", "mutating": true },
  { "command": "integrations.connect", "permission": "integrations.connect", "tier": "T3", "mutating": true },
  { "command": "integrations.disconnect", "permission": "integrations.disconnect", "tier": "T3", "mutating": true },
  { "command": "admin.users.manage", "permission": "admin.users.manage", "tier": "T3", "mutating": true },
  { "command": "admin.policy.update", "permission": "admin.policy.update", "tier": "T3", "mutating": true },
  { "command": "admin.audit.read", "permission": "admin.audit.read", "tier": "T1", "mutating": false },
  { "command": "jobs.read", "permission": "jobs.read", "tier": "T0", "mutating": false },
  { "command": "jobs.cancel", "permission": "jobs.cancel", "tier": "T1", "mutating": true }
]' "$force"

  # 5. Concentric Layer READMEs
  local layers=("contracts" "core" "bridge" "state" "ui" "security" "observability" "intent" "platform" "features" "app")
  for layer in "${layers[@]}"; do
    write_file "$p_root/src/$layer/README.md" "# Concentric Layer: $layer

Complies with Engineering Law L-01 (Inward/Downward dependency flow only).
Reference: PipelineSolution_Frontend_Engineering_Codebase_Guide.md" "$force"
  done

  # 6. Features
  local features=("_template" "dashboard" "diagnostics" "gmail" "jobs" "payments" "session" "sheets")
  for feat in "${features[@]}"; do
    local f_dir="$p_root/src/features/$feat"
    mkdir -p "$f_dir/api" "$f_dir/components" "$f_dir/hooks" "$f_dir/screens"
    write_file "$f_dir/feature.manifest.ts" "export const manifest = {
  name: '$feat',
  version: '1.0.0',
  description: 'Feature module for $feat'
};" "$force"
  done
}
