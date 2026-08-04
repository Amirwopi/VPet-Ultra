# Steam Removal

این صفحه جزئیات کامل حذف وابستگی‌های Steam از VPet-Ultra را توضیح می‌دهد.

## What Was Removed

### NuGet Packages
- `Facepunch.Steamworks` (x64)
- `Facepunch.Steamworks.win32` (x86)

### Binary Files
- `steam_api.dll`
- `steam_api64.dll`

### Code Modules
- `MutiPlayer/` directory (10 files) — entire multiplayer module
  - `winMutiPlayer.xaml` / `.cs` — lobby window
  - `MPFriends.xaml` / `.cs` — friend list
  - `MPUserControl.xaml` / `.cs` — user control
  - `winMPBetterBuy.xaml` / `.cs` — multiplayer shop
  - `MPController.cs` — controller
  - `MPMOD.cs` — mod integration
- `MutiPlayer/` interfaces (Interface project)
  - `IMPWindows.cs`
  - `IMPFriend.cs`
  - `MPMessage.cs`

### Steam Features Removed

| Feature | API Used | Replacement |
|---------|----------|-------------|
| Steam Init/Shutdown | `SteamClient.Init/Shutdown` | None (not needed) |
| Workshop Loading | `Steamworks.Ugc.Query.ItemsReadyToUse` | Local mod path only |
| Mod Publishing | `Editor.NewCommunityFile.SubmitAsync` | "Feature unavailable" message |
| Cloud Saves | `SteamRemoteStorage.FileWrite/Read` | Local save files only |
| Leaderboards | `SteamUserStats.FindOrCreateLeaderboardAsync` | Removed (no ranking) |
| Rich Presence | `SteamFriends.SetRichPresence` | Removed |
| Screenshots | `SteamScreenshots.AddScreenshot` | Local screenshot only |
| Avatar | `SteamFriends.GetLargeAvatarAsync` | Removed |
| Auth Key | `SteamUserStats.FindLeaderboardAsync` | Removed |
| Lobby/P2P | `SteamMatchmaking`, `SteamNetworking` | Removed (no multiplayer) |
| User Identity | `SteamClient.SteamId`, `SteamClient.Name` | `Environment.UserName` |
| Diagnosis Upload | `steamid` parameter in URL | Removed parameter |

### Files Modified

| File | Changes |
|------|---------|
| `VPet-Simulator.Windows.csproj` | Removed PackageReferences + DLL None items |
| `MainWindow.xaml.cs` | Removed Steam init, workshop, lobby, shutdown |
| `MainWindow.cs` | Removed Handle_Steam, cloud saves, rich presence, auth key, leaderboard |
| `MainWindow_Property.cs` | Removed SteamID/SteamAuthorID properties |
| `winGameSetting.xaml.cs` | Removed workshop editor, publish buttons, Steam rich presence |
| `winGameSetting.xaml` | Removed StartUpSteamBox, ButtonPublish, ButtonSteam |
| `winCharacterPanel.xaml.cs` | Removed leaderboard, screenshots, avatar |
| `winCharacterPanel.xaml` | Removed cb_AgreeUpload checkbox |
| `winReport.xaml.cs` | Removed IsSteamUser guard, steamid parameter |
| `winSaveManager.xaml.cs` | Removed SteamRemoteStorage cloud saves |
| `Setting.cs` | Removed StartUPBootSteam, SteamID, SteamFriends |
| `CoreMOD.cs` | Removed steam_api/Facepunch from ignored DLLs |
| `ExtensionFunction.cs` | Removed Steamworks/Facepunch from license list |
| `IMainWindow.cs` | Removed IsSteamUser, SteamID, SteamAuthorID, MutiPlayerHandle |
| `MWController.cs` | Removed winMutiPlayer references |

## Backward Compatibility

`IsSteamUser` property به‌صورت `false` ثابت نگه‌داشته شده:

```csharp
public bool IsSteamUser { get; } = false;
```

این به‌دلیل این است که کدهای پلاگین شخص ثالث ممکن است به این property ارجاع دهند. با `false`، تمام `if (IsSteamUser)` branches به‌صورت خودکار مسیر non-Steam را طی می‌کنند.

## What Still Works

- ✅ Pet animation و display
- ✅ Save/load (local files)
- ✅ Mod loading (local path)
- ✅ Food/work/sleep system
- ✅ Character panel (بدون ranking)
- ✅ Settings
- ✅ Report/feedback (بدون Steam guard)
- ✅ Live2D plugin
- ✅ Code plugins

## What Doesn't Work

- ❌ Steam Workshop (download/upload mods)
- ❌ Steam Cloud saves
- ❌ Multiplayer (lobby/P2P)
- ❌ Leaderboards/rankings
- ❌ Steam rich presence
- ❌ Steam screenshots upload
- ❌ Mod publishing to Workshop