# Security Policy

## Reporting a Vulnerability
Report security vulnerabilities directly to the security team. Do not open public issues for security exploits.

## Security Boundary Summary
- **App user / IPC / Host:** Protected by authentication, rate limits, schema validation, and authorization pipeline.
- **In-process secrets:** Access tokens are ephemeral in memory; refresh tokens protected via Windows DPAPI; high-risk PayMongo secrets stay off local machine.
- **SSRF & Egress:** Managed exclusively by `EgressAllowlistHandler` with pinned hostnames.
- **Capability Isolation:** Single-use cryptographic `AuthorizationTicket` prevents unauthorized connector access.
