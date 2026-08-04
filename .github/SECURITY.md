# Security Policy

## Supported Versions

| Version | Supported |
|---------|-----------|
| latest (main) | ✅ |
| tagged releases | ✅ |
| older forks | ❌ |

## Reporting a Vulnerability

If you discover a security vulnerability in VPet-Ultra:

1. **Do NOT open a public issue.**
2. Email the maintainer directly or use [GitHub Security Advisories](https://github.com/Amirwopi/VPet-Ultra/security/advisories/new).
3. Include:
   - Description of the vulnerability
   - Steps to reproduce
   - Potential impact
   - Suggested fix (if any)

You will receive a response within 48 hours.

## Security Considerations

VPet-Ultra runs AI models locally and optionally connects to external LLM endpoints. Key security boundaries:

- **Local inference** — no data leaves the machine
- **Online mode** — API keys stored in `appsettings.json`, never logged
- **Agent tools** — path validation, command blocklist, permission gating
- **Secret redaction** — secrets are redacted from logs automatically
- **Path traversal prevention** — all file operations are sandboxed to allowed directories