# ADR 0004: SQLite as Memory Store

## Status
Accepted — 2026-08-01

## Context
The memory system needs a persistent store that supports:
- Episodic and semantic memory types.
- Embedding-based cosine-similarity retrieval.
- Importance decay and consolidation.
- Offline operation (no external database server).
- Low overhead on desktop hardware.

## Decision
**SQLite** (via `Microsoft.Data.Sqlite` + Dapper) is the memory store backend.
Embeddings are stored as JSON arrays in a TEXT column. Cosine similarity is computed
in-process after retrieval. This avoids the need for a vector database extension
(sqlite-vec, FAISS) in the initial implementation.

## Consequences
- + Zero-config: SQLite is a single file, no server to install.
- + Offline-first: works without internet or external services.
- + Portable: the database file can be copied, backed up, or exported as JSON.
- − Cosine similarity in-process is O(n) per query — fine for thousands of memories,
  may need optimization (sqlite-vec or FAISS) for larger stores.
- − JSON embedding storage is space-inefficient; a future migration to BLOB or
  a native vector extension would reduce storage.