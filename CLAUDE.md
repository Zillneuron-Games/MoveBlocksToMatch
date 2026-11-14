# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project overview

"MoveBlocksToMatch" (shipped as **Blockmates**; runtime namespace `Zillneuron.Blockmates`) is a sliding-block puzzle game built in Unity, made by Zillneuron. The player slides "inscription" blocks (Red/Blue + a third color that alternates Green/Yellow by level parity) across a 7x5 tile board onto matching target tiles. It has an A*-based hint system that spends purchasable hints, guest play or platform sign-in (Google Play Games / Sign in with Apple) with Unity Cloud Save sync, in-app purchases, LevelPlay ads, and EN/FR/ES localization. Targets are Android and iOS.

## Development environment

- **Unity version:** `6000.6.3f1` (revision `45d8eee7de74`). `ProjectSettings/ProjectVersion.txt` is authoritative.
- There is no CLI build/test workflow. Open the project in the Unity Editor, or in Rider/Visual Studio via `MoveBlocksToMatch.slnx`. The `.csproj` files are Editor-generated.
- Game code lives in `Assets/Scripts` and compiles into `Assembly-CSharp` (C# 9). `Assets/Settings/GlobalSettings.cs` is part of the same assembly.
- Render pipeline: URP. Input uses the legacy `UnityEngine.Input` API (see `InputManager`).
- Build scenes: only `Assets/Scenes/SampleScene.unity`. Everything (loading, menus, gameplay) runs in that scene through view switching.
- Tests: the game itself has no tests. Only the Zillneuron packages contain test assemblies. `ProgressSync.Runtime.csproj` / `ProgressSync.Tests.csproj` are stale generated files that point at a deleted `Assets/Scripts/ProgressSync` folder; ignore them.
- Platform setup for sign-in (Play Console, Apple Developer, Unity Authentication) is documented in `PLATFORM-SIGN-IN.md`.

## Packages

Source of truth: `Packages/manifest.json` plus `Packages/packages-lock.json`.

| Package | Version / source | Used for |
|---|---|---|
| `com.unity.render-pipelines.universal` | 17.6.0 | Rendering |
| `com.unity.ugui` | 2.6.0 | UI (Canvas, TextMeshPro) |
| `com.unity.services.authentication` | 3.8.0 | Player identity |
| `com.unity.services.cloudsave` | 3.4.0 | Player data sync |
| `com.unity.purchasing` | 5.4.3 | IAP (wrapped by `com.zillneuron.inapppurchase`) |
| `com.unity.services.levelplay` | 9.5.1 | Ads (wrapped by `com.zillneuron.ads`) |
| `com.lupidan.apple-signin-unity` | git, `v1.4.4` | Sign in with Apple (iOS) |
| Google Play Games plugin | imported into `Assets/GooglePlayGames` (not a UPM package) | Android sign-in |

Android dependencies are resolved by `Assets/MobileDependencyResolver`. `Assets/Plugins/Android` holds custom Gradle templates and the `GooglePlayGamesManifest.androidlib`. `Assets/Resources/BillingMode.json` sets the Android store to Google Play.

### Zillneuron packages: embedded vs Git

All five are listed in `manifest.json` with Git URLs, but two of them also exist as folders inside `Packages/`. Unity resolves a package from a folder in `Packages/` in preference to the manifest URL, and the lock file confirms this:

| Package | Resolved from | Where to edit |
|---|---|---|
| `com.zillneuron.uilayout` | **embedded** (`Packages/com.zillneuron.uilayout`) | Edit in place; changes are part of this repo |
| `com.zillneuron.ads` | **embedded** (`Packages/com.zillneuron.ads`) | Edit in place; changes are part of this repo |
| `com.zillneuron.utilities` | Git (cached under `Library/PackageCache`) | Read-only here; changes belong in its own repo |
| `com.zillneuron.inapppurchase` | Git (cached under `Library/PackageCache`) | Read-only here |
| `com.zillneuron.web` | Git (cached under `Library/PackageCache`) | Read-only here |

If an embedded folder is removed, Unity falls back to the manifest's Git URL. Read Git package sources from `Library/PackageCache/<name>@<hash>`, but never edit them there.

Shared base types from `com.zillneuron.utilities`:
- `MonoSingleton<T>`: `Instance` finds an existing component in the scene, or **creates a new GameObject** if none exists.
- `AService<T>`: a `MonoSingleton` that calls `DontDestroyOnLoad` and `Initialize()` when its instance is created. `AdsService` and `InAppPurchaseService` are `AService`s, so they initialize lazily the first time `Instance` is accessed.

**Security note:** two manifest URLs (`com.zillneuron.uilayout` and `com.zillneuron.utilities`) still have GitHub credentials embedded in them. These should be rotated and replaced with a credential-free auth method (same issue as in the `Blot` project). The other Git dependencies use plain URLs. Never copy credential values into docs, logs, or commits.

## Architecture

### Configuration (`Assets/Settings/GlobalSettings.cs`)

`GlobalSettings` is a ScriptableObject (`Assets/Settings/GlobalSettings.asset`) assigned to the scene's `SettingsManager` and read through `SettingsManager.Instance.Settings`. It holds:
- new-player defaults: `StartingHints`, sound, music, language
- gameplay tuning: `LevelsCount`, `BlockMovementSpeed`, `StoppingDistance`, `ResultsDelay`, `MaximumSteps`, `SwipeThreshold`
- shop: `FromAdsProductsCount`, `StoreProducts` (`ProductId`, `PurchaseType`, `GrantAmount`)
- `CloudReconciliationLimit` (maximum Cloud Save sync retry loops)

Put new tunables here rather than hard-coding them.

### Startup and UI flow (`UI/`)

The UI is built on `Zillneuron.UILayout`:
- `ViewContext` is the singleton that switches views and opens dialogs.
- Full-screen views derive from `ACompleteView`; `LoadingView` derives from `StartView`.
- Views navigate with `ChangeActivityToNext<T>()`, `ChangeActivityToNextAndReplace<T>()` and `ChangeActivityToPrevious()`.
- Dialogs open with `ViewContext.Instance.OpenDialog<T>(parameters)` and close with `HideDialog<T>()`. Each dialog has a matching `*Parameters` class: `SettingsDialog`, `ShopDialog`, `GameOverDialog`, `ResultDialog`, `MenuDialog`, plus the package's `AlertDialog`.
- `SetTexts()` overrides pull localized strings with `LocalizationManager.Instance.GetPhrase(PhraseCollection.X)`.

Flow:
1. `GameplayManager.Start()` calls `ViewContext.Instance.Construct()`.
2. `LoadingView` awaits `AccountService.InitializeAsync()`.
3. If `AccountService.NeedsWelcome`, it opens `WelcomeView` (choose "log in" or "play as guest", then localization loads). Otherwise it loads localization and opens `MainMenuView`.
4. `MainMenuView` leads to `GameplayView`, `LevelsView`, `SettingsDialog` and `ShopDialog`.
5. `GameplayView` subscribes to `GameplayManager` events (`onStartGame`, `onStepsUpdate`, `onShowHint`, `onShowMenu`, …) and calls `gameplayManager.StartGame()`.
6. On-screen buttons feed `InputManager.ButtonPressed`, so buttons, swipes and keyboard all arrive as `EInputEvent`s.

`UIManager` is a leftover stub that only logs to the console; the real UI is the view layer.

### Game loop orchestration (`Manager/GameplayManager.cs`)

`GameplayManager` (MonoBehaviour, implements `ITileObjectProvider`) is started explicitly via `StartGame()`, not from its own `Start()`. `LoadScene()`:
1. builds `LevelStartData.CreateInstance()`, which loads every level's `LevelData` plus the player's progress from `AccountService`
2. constructs a 7x5 `GameBoardGrid`
3. calls `CreateGame(NextToLoadGame)`

`CreateGame` branches on `LevelData.IsDoubleGame` (true when `Green == GreenTarget`, i.e. the level has no third color):
- **Double game:** only Red/Blue `InscriptionBlock`s + their `TargetBlock`s + optional mobile/static blocks → constructs `DoubleGame`.
- **Triple game:** Red/Blue plus a third color chosen by `ChooseRandomBlock` (Green if the level id is even, Yellow if odd) → constructs `TripleGame`.

Input is remapped because the board is rotated relative to the input scheme: `EInputEvent.Up → EDirection.Left`, `Down → Right`, `Left → Down`, `Right → Up`. `Back` (and `Escape` during play) undoes a move. `gameplayState` (`EGameplayState`: Start/Gameplay/Transit/Pause/Win/End/Error) drives `Update()`. In `Transit`, `UpdateTransit()` animates blocks using `GlobalSettings.BlockMovementSpeed` / `StoppingDistance`.

On a win, `LevelStartData.UpdateDynamicData` records the attempt through `AccountService.RecordAttempt` and advances the next level with `AccountService.SetNextGameId`.

### Game variants (`Game/AGame.cs`, `DoubleGame.cs`, `TripleGame.cs`)

`AGame` is the abstract base. It is structurally similar to the sibling `MoveBlocksToConnect`/`MoveBlocksToEscape` projects, but built around two or three `InscriptionBlock`s and matching `TargetBlock`s rather than colored tile groups. It holds:
- `stepsMinimum` (per-level par, from `LevelData.MinimumStepsCount`)
- the `allMoves` undo stack of `GameplayStep`s
- `gameBoardGrid` and `gameDataDynamic`

Undo is capped by `GlobalSettings.MaximumSteps`. `CalculateCoins()` computes a per-game coin value from the level id and how close `StepsCount` is to `stepsMinimum`; coins are only displayed and are not persisted. `GetBoardState()` (abstract) converts current block positions into a `Board` for the hint solver.

Each `MoveUP/DOWN/LEFT/RIGHT` uses the same slide-until-blocked loop as the sibling projects:
1. an outer `for` over `allMovableBlocks.Count` plus an inner `foreach`, so chained pushes propagate
2. push a `GameplayStep` onto `allMoves`
3. fire `ThrowFinalTransformEvent()`

After the transit animation, `MoveBlockObjects` checks whether every inscription block sits on its target tile (win → `eventBlocksMatch`). It raises `EErrorType.StepsCount` when the step count exceeds `MaximumSteps`. `DoubleGame` and `TripleGame` differ only in having two or three inscription/target pairs.

### Grid model (`Game/GameBoardGrid.cs`, `Game/Tile.cs`)

Same reference-point graph approach as the sibling projects, but using `Tile`/`ETileNeighborSide`/`ETileState` naming instead of `GridElement`/`EGridElementNeighborSide`/`EGridElementState`. `ITileObjectProvider.GetTileObject(x, y)` (implemented by `GameplayManager` over `gridBlocks`) resolves the visual `GameObject` for each tile.

**`Vector2Int` is a project type:** `Util/Vector2Int.cs` declares a global-namespace `Vector2Int` struct that shadows `UnityEngine.Vector2Int`. It has an implicit conversion to `Vector2` and an explicit conversion from it. Block positions, `LevelData` and the hint solver all use this struct.

### Blocks (`Block/`)

`ABlock` is the base (`Id`, `IsMovable`, `IsInTransit`, `CurrentTile`, `BlockPosition`). Concrete types:
- `InscriptionBlock`: the colored, player-moved pieces
- `TargetBlock`: destination tiles
- `MobileBlock` / `StaticBlock`: obstacles, same as the sibling projects

### Hint system (`Hint/`)

`new Hint(board).GetAStarHint()` runs an A* search over `Board`:
- `Pawns` = current inscription positions
- `Blocks` = static obstacle positions
- `Targets` = target positions

`State` nodes track cost so far plus a Manhattan-distance heuristic to the targets. `SimulateMove` replicates the slide-until-blocked movement rule for each of the four directions to generate successor states. `ReconstructFirstMove` walks back to the first move from the start state. The method returns a `Direction2D` vector, which `GameplayManager.GetHint()` maps to an `EDirection`.

`HandleHintButtonClick` only shows a hint (`onShowHint`) when `AccountService.IsReady` and `AccountService.TrySpendHints()` succeeds, so each hint costs one hint from the player's inventory.

### Level data (`Game/Data/`)

- `LevelData`: per-level DTO loaded by `LevelDataReader.GetGameData(id)` from `Resources/Data/{id}.json` (files `1.json`–`105.json` exist; the count used at runtime is `GlobalSettings.LevelsCount`). Fields: `Id`, `StageId`, `GameId`, `MinimumStepsCount`, `Red`/`Blue`/`Green` start positions, `RedTarget`/`BlueTarget`/`GreenTarget`, optional `Mobiles`/`Statics` lists. `IsDoubleGame` is derived.
- `GameDataDynamic`: per-level progress (`Id`, `BestAttempt` = best step count, `NumberOfGamesPlayed`).
- `LevelStartData`: built at game start. It loads all `LevelData` up front and takes progress from `AccountService.Levels` / `NextLevel`.
- The old PlayerPrefs code in `LevelDataReader` is commented out. All persistence now goes through `Services/SignIn`.

### Accounts, persistence and sync (`Services/SignIn/`, namespace `Zillneuron.Blockmates.Services.SignIn`)

Each class has a single responsibility, and they should stay separated that way:

| Class | Responsibility |
|---|---|
| `SignInService` | Identity only. Unity Services init, Google Play Games (Android) / Sign in with Apple (iOS) credential → Unity Authentication, session restore, sign-out. Editor and other platforms throw `PlatformNotSupportedException`. |
| `DataStoreService` | PlayerPrefs JSON only, one slot per player ID. Keys: `level_data_<id>`, `settings_data_<id>`, `inventory_data_<id>`. |
| `DataSyncService` | Cloud Save requests only. Keys: `level_data`, `settings_data`, `inventory_data`. Uses write locks; with no existing record it does a plain save. |
| `ProgressRules` | Level normalization, unlock rules and merging. Levels merge monotonically: best positive `BestAttempt`, max `NumberOfGamesPlayed`. |
| `SettingsData` / `InventoryData` | Records with per-field UTC-tick timestamps. Settings merge each field independently (sound, music, language); inventory (`HintsCount`) merges last-writer-wins. |
| `AccountService` | Coordinator: selected profile, levels/settings/inventory state, events (`OnLevelsChanged`, `OnSettingsChanged`, `OnInventoryChanged`, `OnAccountChanged`, `OnSynchronizationFailed`). |

Account rules (from `AccountService`):
- **Guest:** a local-only slot (`offline_guest_local_only`) that never contacts Unity Services.
- **Log in:** for a new account (or an interrupted registration), the guest's settings and inventory carry over. For a returning account, cloud data is merged into the local account save. Guest levels are merged in either case, and then the guest slot is deleted.
- **Log out:** keeps the account save on the device and opens a fresh guest.
- **Pending uploads:** every local change marks the account pending (`account_pending_<id>`). `SyncAsync` downloads, merges, uploads and re-verifies all three records, retrying on Cloud Save `Conflict` up to `CloudReconciliationLimit` times, and clears the pending flag only after verification succeeds.
- **Next level:** the next-level pointer (`account_nextGameId.<id>`) is stored in PlayerPrefs only and is not synced.
- **iOS build step:** `AppleSignInBuild` (a post-process build step for iOS) adds the Sign in with Apple capability to the generated Xcode project.

### Localization (`Manager/LocalizationManager.cs`)

`LocalizationManager.Initialize(ELanguageCode)` loads `StreamingAssets/Translations/{EN|FR|ES}.json` (a `TranslationData` with `Phrases[]` key/value pairs) into a dictionary:
- On Android it reads through `UnityWebRequest`, because StreamingAssets are packed inside the APK.
- Elsewhere it uses `File.ReadAllTextAsync`.

The current language comes from `AccountService.LanguageCode`. Phrase keys live in `UI/PhraseCollection.cs`.

### In-app purchases (`Manager/PurchaseManager.cs`, `com.zillneuron.inapppurchase`)

Actual flow, as implemented:

1. **Store init.** `InAppPurchaseService` (lazy `AService`) loads `Resources/InAppPurchaseSettings` (`InAppPurchaseSettingsScriptableObject`, which builds the catalog from `GlobalSettings.StoreProducts`). It then initializes Unity Purchasing through `InAppPurchaseRunner`, which uses the legacy `UnityPurchasing.Initialize` / `IDetailedStoreListener` API. In the Editor it always uses the fake store.
2. **Shop UI.** `ShopDialog` shows slot 0 as the "from ads" item and fills the remaining slots from the store controller's products, using localized titles and prices.
3. **Buy.** `PurchaseManager.BuyFromStore(productId)`:
   - It requires `AccountService.IsReady` and no other store purchase in progress.
   - It looks up the product in `GlobalSettings.StoreProducts`.
   - If `GrantAmount > 0`, it calls `AccountService.TryBeginHintPurchase()`, which blocks account switching until the purchase finishes.
   - It then calls `InAppPurchaseService.BuyProduct`.
4. **Store callback.** `ProcessPurchase` matches the purchase to a pending request by product ID, invokes the callback **synchronously** with the transaction ID, and then returns `PurchaseProcessingResult.Complete`.
5. **Fulfillment.** On `Succeeded` with a non-empty transaction ID and a matching product ID, `AccountService.GrantPurchasedHints(amount, transactionId)` runs:
   - It skips the grant if PlayerPrefs already has `hint_purchase_<transactionId>` (local de-duplication).
   - Otherwise it adds the hints, writes the marker, and persists the inventory locally, which marks the account pending.
   - It starts a fire-and-forget `SyncAsync` when the cloud is available.
   - Finally `EndHintPurchase()` runs.
6. **Validation.** No receipt validation is performed, either locally (no `CrossPlatformValidator`) or server-side. The transaction ID is only used as the local de-duplication key.

Current limitations, observed and not yet fixed:
- A purchase the store delivers without a matching pending request (for example, one completed after an app restart) is not fulfilled.
- Products with `GrantAmount == 0` grant nothing in code.
- `RestorePurchases` only works on Apple platforms, and its handler only logs.

### Ads (`com.zillneuron.ads` → LevelPlay)

`AdsService` supports **banners and interstitials only**. There are **no rewarded ads** anywhere in the project: there is no rewarded ad unit, API, or reward callback. `AdsSettings` / `AdsSettingsFile` hold app keys plus banner and interstitial unit IDs, loaded with `Resources.Load("AdsSettings")`.

- **Interstitial:** `ShopItemPanel`'s "from ads" item calls `PurchaseManager.GetFromAds(count)`, which subscribes a handler and calls `AdsService.ShowInterstitial()`. The handler only logs the result and unsubscribes; **it grants no hints**.
- **Banner:** `ShowBanner()` / `HideBanner()` exist but are not called by game code.
- **Initialization:** `AdsService` is created on first access, which calls `LevelPlay.Init` (the Editor uses mock IDs). No game code calls `InitializeSettings(...)` or `RequestAds()`, so banners and interstitials stay disabled and `ShowInterstitial()` returns immediately. No `AdsSettings` asset is present in `Assets/Resources`.

If rewarded ads are added, they need new API in `AdsService` and must grant through `AccountService.AddHints`. Don't build on the interstitial path as if it already grants rewards.

## Async and main-thread rules

- `AccountService`'s public async methods (`InitializeAsync`, `PlayAsGuestAsync`, `LogInAsync`, `LogOutAsync`, `SyncAsync`) must be called from the Unity main thread. They are serialized by a `SemaphoreSlim` (`operationGate`), so only one account operation or sync runs at a time.
- Game code never uses `ConfigureAwait(false)` or `Task.Run`. Continuations resume on Unity's synchronization context, which makes PlayerPrefs, Unity Services and scene objects safe to touch after an `await`. Keep it that way.
- UI entry points are `async void`: `LoadingView.InitializeServices`, `WelcomeView.ChooseAccountAsync`, `SettingsDialog.ChangeAccountAsync` / `ChangeToNextLanguage`. Wrap their bodies in try/catch and report failures through `AlertDialog`, as `WelcomeView` does.
- Fire-and-forget syncs (`_ = SyncAsync(...)` after a hint purchase, plus `OnApplicationPause`) rely on `SyncAsync` catching every exception internally (it logs and raises `OnSynchronizationFailed`). Data is always persisted locally first, so a failed sync retries later through the pending flag.
- `SignInService` wraps platform callbacks in `TaskCompletionSource(RunContinuationsAsynchronously)` with a 2-minute timeout. Google authorization codes are single-use, so a new one is requested before account creation.
- IAP init notifications are deferred one frame through a coroutine. The purchase callback runs synchronously inside `ProcessPurchase`, so grants are saved before the store transaction is completed.
- Gameplay itself (`GameplayManager`, `AGame`) is synchronous: an `Update()` state machine plus coroutines.

## Conventions

- Prefixes: `A*` for abstract bases (`AGame`, `ABlock`), `E*` for enums (`EDirection`, `EGameplayState`), `I*` for interfaces.
- Singletons: managers and services use `MonoSingleton<T>`. Older classes such as `UIManager` and `InputManager` use their own `Instance` pattern.
- Platform code: guard it with `#if UNITY_ANDROID && !UNITY_EDITOR` / `#if UNITY_IOS && !UNITY_EDITOR`. Keep Android and iOS behavior in parity.
- Service classes moved into `Zillneuron.Blockmates.Services.SignIn` carry `[MovedFrom]` attributes so existing scene references keep working. Don't remove them.
- Persisted formats: PlayerPrefs keys, Cloud Save keys and the JSON shapes (`LevelsData`, `SettingsData`, `InventoryData`) are live player data. Change them only with a migration.
