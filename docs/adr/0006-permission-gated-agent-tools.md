# ADR 0006: Permission-Gated Agent Tools

## Status
Accepted — 2026-08-01

## Context
The autonomous agent can execute tools (open apps, read files, open URLs, clipboard).
Some tools are safe (read a file in an allowed directory) while others are potentially
dangerous (open an arbitrary application, run a command). The system needs a permission
layer that:
- Auto-grants safe operations.
- Prompts the user for dangerous operations.
- Remembers grants (once vs always).
- Allows revocation.

## Decision
A `DefaultPermissionService` gates all tool execution. Each `ToolSchema` declares
`RequiresConfirmation` and `AllowAlways`. The agent runtime checks permission before
executing any tool call. If permission is `NotRequested` and the tool doesn't require
confirmation, it's auto-granted. If confirmation is required, the system emits a
`PermissionPromptedEvent` (the UI layer shows a dialog).

Additionally, `PathValidator` prevents path traversal in file tools, and
`CommandValidator` blocks dangerous commands (rm -rf, format, shutdown, etc.).

## Consequences
- + Safe-by-default: dangerous tools require explicit user consent.
- + Path traversal and dangerous commands are blocked at the validation layer.
- + Permission grants are revocable.
- − The permission prompt UI is a stub in Phase 5; full dialog requires WPF integration.
- − In-memory grants are lost on restart; persistence to SQLite is a future enhancement.