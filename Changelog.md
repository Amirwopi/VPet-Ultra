# Changelog

All notable changes to VPet-Ultra are documented here.
Format based on [Keep a Changelog](https://keepachangelog.com/).

## [Unreleased]

## [0.2.0] — 2026-08-04

### Added
- Live2D animation plugin (`1102_Live2DAnimation`) — supports `.moc3` models
- GitHub Issue templates (bug report, feature request)
- GitHub PR template
- Code of Conduct, Security Policy, Funding config
- Persian README with English technical terminology
- `.NET 8.0.423 SDK` installed permanently on build server

### Removed
- **Steam dependency completely removed** — standalone mode
  - `Facepunch.Steamworks` NuGet package
  - `steam_api.dll` / `steam_api64.dll` binaries
  - Steam Workshop loading (local mod path only)
  - Steam Cloud saves
  - Steam Multiplayer (Lobby/P2P) — entire `MutiPlayer/` module deleted
  - Steam Leaderboards/Rankings
  - Steam Rich Presence
  - Steam Screenshots
  - Mod publishing to Workshop
  - `MutiPlayer/` interfaces (`IMPWindows`, `IMPFriend`, `MPMessage`)
- All Chinese text from `.cs`/`.xaml` files (3000+ lines of comments + string literals)
- Chinese `.md` files (`README_zht`, `CONTRIBUTING_zht`, `README_en`, `README_ja`)
- `docs/` directory (auto-generated architecture docs)
- `memory-bank/` directory
- 10 broken mod symlinks pointing to Windows paths

### Changed
- All UI text, comments, and documentation converted to English
- `IsSteamUser` property kept as `false` constant for backward compatibility
- `winGameSetting` — Workshop publish buttons replaced with "feature unavailable" messages
- `winCharacterPanel` — Steam leaderboard/ranking code replaced with no-op
- `winSaveManager` — Steam Cloud save entries removed
- `winReport` — Steam user guard removed, feedback available to all users
- `MainWindow` — Steam init/shutdown, rich presence, auth key generation removed
- `CoreMOD` — Steam DLL names removed from ignored files list
- `ExtensionFunction` — Steamworks/Facepunch removed from license list

### Fixed
- App stuck on loading screen — broken mod symlinks caused `EnumerateDirectories()` to throw unhandled exception
- `try/catch` added around mod directory enumeration for resilience

## [0.1.0] — 2026-07-31

### Added
- Initial fork from [VPet](https://github.com/LorisYounger/VPet)
- AI Cognition layer (LLamaSharp, memory, personality, emotion, voice, agent)
- GitHub Actions build workflow (Windows)
- Git history squashed (1306 → 57 commits)