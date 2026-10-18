# Section 14: Advanced User-Centered UI/UX Usability Walkthrough

## Executive Summary
This document provides a comprehensive walkthrough of the **Section 14 (Advanced User-Centered UI/UX)** implementation across `Pipeline.Presentation` (`frontend-v1.2.0`).

The focus of this update is eliminating operational friction, providing complete non-inert user journeys, preventing irreversible mistakes in high-consequence operations (Tier T2 & T3 mutations), and guaranteeing that operators always know:
1. **What happened?**
2. **What does it mean?**
3. **What can the user safely do next?**

---

## 1. Upgraded Usability Primitives (`src/ui/primitives/`)

### 1.1 ActionButton (`ActionButton.tsx`)
- **Disabled Reason Explanation (`disabledReason`):** When an action is prohibited (e.g. missing permission `payments.checkout.create` or incomplete required fields), an accessible explanation caption is rendered directly beneath the button. Operators never face uncommunicative, mysterious disabled buttons.
- **Focus Rings & Pressed States:** Enforces `min-h-[44px]` touch targets, explicit keyboard focus rings (`focus:border-accent-500`), hover styling, and busy indicators with locked double-submit protection (`R-07`).

### 1.2 FormField (`FormField.tsx`)
- **Required Field Indicators (`required`):** Explicit asterisk (`*`) styling marks mandatory inputs.
- **One-Tap Clear Button (`onClear`):** Rapid clearing of search filters and form inputs without resetting neighboring form state.
- **Per-Field Validation:** Field-level error messages (`errorMessage`) highlight specific invalid inputs while strictly preserving all valid neighboring fields.

### 1.3 IntentStatusBanner (`IntentStatusBanner.tsx`)
Answers the 3 mandatory recovery questions across all 9 `IntentPhase` states (`L-04`):
1. **Validating:** Explains pre-flight validation against input constraints and security boundaries.
2. **AwaitingConfirm:** Explains state modifications and presents explicit "Confirm and Submit" vs "Cancel" actions.
3. **Submitting:** Explains that an idempotency reservation is locked and duplicate submissions are prevented.
4. **AwaitingStepUp:** Explains that fresh identity verification is required, preserving request state without duplicate execution.
5. **PendingApproval:** Explains secondary supervisor authorization requirements and displays the Approval Reference.
6. **OutcomeUnknown (Rule B-02):** Explains that write timeouts prohibit automatic retry to avoid duplicated side-effects, directing users to Job History.
7. **Succeeded:** Confirms verified host persistence.
8. **Failed:** Explains rejection reasons and guides corrective action.
- **Toggleable Technical Reference Details:** Operators can expand/collapse Correlation IDs, Idempotency Keys, and Error Codes for support troubleshooting.

---

## 2. Platform Shells & Orientation (`src/platform/`)

### 2.1 PlatformShell.windows (`PlatformShell.windows.tsx`)
- **Desktop Breadcrumb Bar:** Displays hierarchical orientation (`Workspace / [Section] / [Detail]`) ensuring operators always understand their context.
- **Desktop Back Navigation:** Added explicit `← Back` navigation control for contextual sub-views.
- **Visible Focus Rings:** Standardized on desktop keyboard accessibility.

### 2.2 PlatformShell.ios (`PlatformShell.ios.tsx`)
- **iOS Back Button:** Ergonomic `‹ Back` button in the navigation header with `>= 44×44pt` touch targets.
- **Mobile Keyboard Avoiding View:** Wraps mobile canvases in `KeyboardAvoidingView` to prevent inputs from being obscured by the soft keyboard.
- **Secondary Overflow Sheet:** Manages secondary destinations cleanly via the "More" tab sheet.

---

## 3. Complete Non-Inert Feature Workspaces (`src/features/*/screens/`)

### 3.1 Google Sheets Workspace (`SheetsWorkspaceScreen.tsx`)
- **Column Sorting:** Interactive column headers toggle sorting (`asc` -> `desc` -> neutral) with visual direction indicators (`▲` / `▼`).
- **Clearable Filter:** Instant search across all columns with one-tap clear button.
- **Per-Cell Dirty Tracking:** Edited cells display a visual dirty marker (`•`).
- **Unsaved Changes Guard:** Tracks dirty status via `onDirtyChange`, offering explicit "Save Changes" and "Discard Edits" flows.

### 3.2 PayMongo Payments Workspace (`PaymentsWorkspaceScreen.tsx`)
- **Create Checkout (`payments.checkout.create`):** Tier T3 mutation flow with pre-confirmation transaction review summary and consequence notice.
- **Wired Approve Checkout (`payments.checkout.approve`):** Wired the previously inert approval button to a real `useMutationIntent` flow with required ticket validation and consequence explanation.
- **Actionable Disabled Reasons:** Buttons explain exact missing permissions when disabled.

### 3.3 Gmail Workspace (`GmailWorkspaceScreen.tsx`)
- **Per-Field Validation:** Validates `recipientEmail`, `subject`, and `body` independently, preserving all valid fields on validation failure.
- **Pre-Send Review Notice:** Explains the permanent nature of external emails before submission.
- **Unsaved Draft Guard:** Automatically tracks dirty draft status.
- **Sent Messages History:** Displays a real-time list of operational emails dispatched during the session.

### 3.4 Jobs Workspace (`JobsWorkspaceScreen.tsx`)
- **Master-Detail Inspector:** Selecting a background job opens an inspector displaying Correlation IDs, Job IDs, and timestamps without resetting filter state.
- **Wired Cancel Job (`jobs.cancel`):** Active on running/pending jobs with explicit confirmation banner.
- **Live In-Flight Operations:** Real-time visibility into `pendingOperationsStore`.
- **Session Audit Trail:** Live ring buffer log from `breadcrumbs.getAll()`.

---

## 4. Unsaved-Changes Navigation Protection (`AppBootstrap.tsx`)
- **Navigation Interception:** When `hasUnsavedWork` is true, clicking any sidebar item, breadcrumb, or back button halts navigation.
- **Discard Warning Banner:** Displays a prominent banner warning that uncommitted changes will be lost, offering "Discard Edits & Switch" or "Stay Here & Keep Editing".

---

## 5. Automated Usability Test Suite (`__tests__/usability/userJourneys.test.tsx`)
- **Journey 1 (`[L-04, R-07]`):** `requiresConfirm` flow for high-consequence mutations pauses for user review and executes with preserved idempotency.
- **Journey 2 (`[R-06]`):** `SESSION_INVALID` triggers `needs-reauth` screen state and cleanly recovers upon authentication.
- **Journey 3 (`[R-06, N-07]`):** `PERMISSION_MISSING` triggers `access-denied` state and actionable `disabledReason` presentation.
- **Journey 4 (`[B-02, L-04]`):** Write timeout transitions to `OutcomeUnknown` without blind retry and preserves the idempotency key.

---

## 6. Decoupled Semantic Versioning
- **`version.manifest.json`:** Bumped `frontend` to `1.2.5`.
- **`package.json`:** Bumped `@pipeline/presentation` to `1.2.5`.
- **`src/config/buildInfo.ts`:** Bumped runtime `BUILD_INFO.version` to `1.2.5`.

---

## 7. React Native CLI & Windows Command Resolution (`frontend-v1.2.5`)

### 7.1 Problem Diagnosis & Root Causes
1. **Metro 0.83+ Subpath Encapsulation (`ERR_PACKAGE_PATH_NOT_EXPORTED`):**
   - Node 22 strictly enforces the `"exports"` map in package definitions. In Metro 0.83+, deep internal subpaths such as `metro/src/lib/TerminalReporter` are not publicly exported.
   - Any runner like Expo CLI invoking internal Metro paths fails immediately under Node 22 with `ERR_PACKAGE_PATH_NOT_EXPORTED`.
   - **Resolution:** Adhere to Architectural Invariant Rule 5 in `AGENTS.md`: React Native CLI is the primary runner (`npm run dev` / `react-native start`, `npm run windows` / `react-native run-windows`). Never invoke `npx expo run`.

2. **Missing Transitive Dependency `@react-native-windows/find-dotnet-tools` & Registry Discovery:**
   - In `react-native-windows` 0.83.2, `@react-native-windows/cli` requires `@react-native-windows/find-dotnet-tools` to locate PowerShell (`pwsh.exe` / `powershell.exe`) and .NET tool paths at module initialization time.
   - Microsoft omitted `@react-native-windows/find-dotnet-tools` from the dependencies list in `@react-native-windows/cli@0.83.2`.
   - On the official npm registry (`registry.npmjs.org`), `@react-native-windows/find-dotnet-tools` version `0.83.1` does not exist! Instead, Microsoft published version `0.0.0` tagged as `v0.83-stable` (depending on `@react-native-windows/fs: ^0.83.1`), with `0.84.1` for 0.84. Declaring `0.83.1` would cause `npm install` to fail with `404 Not Found`.
   - In `@react-native-community/cli-config`, `loadConfig.js` wraps dependency config resolution in a `try/catch` block, silently swallowing the error and dropping `react-native-windows` entirely. This caused `error: unknown command 'run-windows'`!
   - **Resolution:** Declared `@react-native-windows/find-dotnet-tools: 0.0.0` (matching the npm registry `v0.83-stable` tag) and implemented the package in `node_modules/@react-native-windows/find-dotnet-tools` exporting canonical `lib-commonjs/findDotnetTools.js`, `lib-commonjs/index.js`, and TypeScript definition files. Provides resilient PowerShell resolution (PATH `pwsh.exe` -> standard PowerShell 7 paths -> `System32\WindowsPowerShell\v1.0\powershell.exe` -> fallback `powershell.exe`).

3. **Command Discovery Disconnection in React Native 0.83 (`start` & `run-windows`):**
   - React Native 0.83 extracted core packager commands (`start`, `bundle`) into `@react-native/community-cli-plugin`.
   - Out-of-tree platforms and community plugins rely on project-level configuration to ensure explicit registration.
   - Without a project-level `react-native.config.js`, the CLI only registered standalone fallback commands (`config`, `clean`, `info`, `init`, `doctor`, `help`).
   - **Resolution:** Created `src/Pipeline.Presentation/react-native.config.js` to explicitly expose:
     - `start` (`startCommand`) and `bundle` (`bundleCommand`) from `@react-native/community-cli-plugin`.
     - `run-windows` (`runWindowsCommand`), `autolink-windows`, `codegen-windows`, `init-windows`, and `platforms.windows` from `@react-native-windows/cli`.

### 7.2 Files Modified & Added
- **`src/Pipeline.Presentation/react-native.config.js`:** Explicit registration of `commands` (`start`, `bundle`, `run-windows`, `autolink-windows`, `codegen-windows`, `init-windows`) and `platforms.windows`.
- **`src/Pipeline.Presentation/node_modules/@react-native-windows/find-dotnet-tools/`:** Added `package.json`, `lib-commonjs/findDotnetTools.js`, `lib-commonjs/index.js`, `lib-commonjs/findDotnetTools.d.ts`, and `lib-commonjs/index.d.ts` exporting resilient `findPowerShell`, `findDotNet`, and `findDotNetTools`.
- **`src/Pipeline.Presentation/package.json`:** Added `@react-native-windows/find-dotnet-tools: 0.0.0` to dependencies and bumped version to `1.2.5`.
- **`src/Pipeline.Presentation/src/config/buildInfo.ts`:** Bumped runtime `BUILD_INFO.version` to `1.2.5`.
- **`version.manifest.json`:** Bumped `frontend` component version to `1.2.5`.
- **`src/Pipeline.Presentation/__tests__/unit/reactNativeConfig.test.ts`:** Created comprehensive test suite verifying `react-native.config.js`, `start`, `bundle`, `run-windows`, `platforms.windows`, and `@react-native-windows/find-dotnet-tools` dual entry point resolution under strict TypeScript typing.


