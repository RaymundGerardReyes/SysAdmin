# ADR-0001: Native Bridge vs WebView2 Navigation

- **Status:** Accepted
- **Related rules:** R-01, B-01, L-01

## Context
The UI uses React Native Windows + iOS + Expo Modules rather than a raw HTML webview.

## Decision
Desktop host exposes an in-process native bridge router (`BridgeCommandRouter` / `IpcMessageRouter`) communicating exclusively with `IUseCaseDispatcher`.
