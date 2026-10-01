# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project overview

"MoveBlocksToMatch" is a sliding-block puzzle game built in Unity 6000.4.10f1 (Unity 6), made by Zillneuron. The player slides "inscription" blocks (Red/Blue + a third color that alternates Green/Yellow by level parity) across a 7x5 tile board onto matching target tiles. 105 levels, with an A*-based hint system, Unity Cloud Save-backed progress, anonymous auth, and localization.

## Development environment

This is a Unity project — there is no CLI build/test workflow. Open in the Unity Editor (6000.4.10f1) or in Rider/Visual Studio via `MoveBlocksToMatch.slnx`.

- `Assembly-CSharp.csproj` is the main runtime assembly.
- Private package dependencies (`com.zillneuron.uilayout`, `com.zillneuron.utilities`, `com.zillneuron.web`) are pulled from private GitHub repos referenced in `Packages/manifest.json`. The UI/view layer is built on `Zillneuron.UILayout` (`ViewContext`, `ACompleteView`, dialogs).
- Backend services: `com.unity.services.authentication` (anonymous sign-in, with stubs for linking Google/Apple accounts) and `com.unity.services.cloudsave` (player progress sync) — see `Services/SignInService.cs` and `Services/DataManagementService.cs`.
- Localization: `LocalizationManager` loads `StreamingAssets/Translations/{languageCode}.json` into a phrase dictionary; UI views call `LocalizationManager.Instance.GetPhrase(PhraseCollection.X)`.

**Security note:** `Packages/manifest.json` contains hardcoded GitHub PATs embedded in the `com.zillneuron.uilayout`/`utilities`/`web` package URLs. These should be rotated and replaced with a credential-free auth method (same issue as in the `Blot` project).

## Architecture

### Game loop orchestration (`Manager/GameplayManager.cs`)

`GameplayManager` (MonoBehaviour, implements `ITileObjectProvider`) is started explicitly via `StartGame()` (called from the UI flow via `ViewContext`, not `Start()`). `LoadScene()` builds `GameStartData` (loads all 105 levels + dynamic/progress data via `DataReader`), constructs a 7x5 `GameBoardGrid`, and calls `CreateGame(gameStartData.NextToLoadGame)`.

`CreateGame` branches on `GameData.IsDoubleGame` (true when `Green == GreenTarget`, i.e. the level has no third color):
- **Double game** — only Red/Blue `InscriptionBlock`s + their `TargetBlock`s + optional mobile/static blocks → constructs `DoubleGame`.
- **Triple game** — Red/Blue plus a third color chosen by `ChooseRandomBlock` (Green if `gameData.Id` is even, Yellow if odd) → constructs `TripleGame`.

Input is remapped: `EInputEvent.Up → EDirection.Left`, `Down → Right`, `Left → Down`, `Right → Up` (the board is rotated relative to the input scheme). `gameplayState` (`EGameplayState`: Start/Gameplay/Transit/Pause/Win/End/Error) drives `Update()`/`UpdateTransit()`.

### Game variants (`Game/AGame.cs`, `DoubleGame.cs`, `TripleGame.cs`)

`AGame` is the abstract base, structurally similar to the sibling `MoveBlocksToConnect`/`MoveBlocksToEscape` projects but built around two/three `InscriptionBlock`s and matching `TargetBlock`s rather than colored tile groups. Holds `stepsMinimum` (per-level par, from `GameData.MinimumStepsCount`), `allMoves` undo stack, `gameBoardGrid`, `gameDataDynamic`. `CalculateCoins()` rewards based on level id and how close `StepsCount` is to `stepsMinimum`. `GetBoardState()` (abstract) converts current block positions into a `Hint.Board` for the A* hint solver.

- **`DoubleGame`** — slides `inscriptionBlockRed`/`Blue` + mobile blocks each move; `IsMatchEnded` when both inscription blocks sit on their respective target tiles.
- **`TripleGame`** — same pattern for three inscription blocks/targets.

Both follow the same slide-until-blocked loop pattern as the sibling projects: `for i in allMovableBlocks.Count` outer loop + inner `foreach` to propagate chained pushes, push a `GameplayStep` onto `allMoves` for undo, fire `ThrowFinalTransformEvent()`.

### Grid model (`Game/GameBoardGrid.cs`, `Game/Tile.cs`)

Same reference-point graph approach as the sibling projects, but using `Tile`/`ETileNeighborSide`/`ETileState` naming instead of `GridElement`/`EGridElementNeighborSide`/`EGridElementState`. `ITileObjectProvider.GetTileObject(x, y)` (implemented by `GameplayManager`) resolves the visual `GameObject` for each tile. Block positions use `Vector2Int` (vs. `Vector2` in the sibling projects).

### Blocks (`Block/`)

`ABlock` base (`Id`, `IsMovable`, `IsInTransit`, `CurrentTile`, `BlockPosition`). Concrete types: `InscriptionBlock` (the colored player-moved pieces), `TargetBlock` (destination tiles), `MobileBlock`/`StaticBlock` (obstacles, same as sibling projects).

### Hint system (`Hint/`)

`Hint.GetAStarHint(board)` runs an A* search over `Board` (`Pawns` = current inscription positions, `Blocks` = static obstacle positions, `Targets` = target positions). `State` nodes track cost-so-far + Manhattan-distance heuristic to the targets; `SimulateMove` replicates the slide-until-blocked movement rule for each of the 4 directions to generate successor states. `ReconstructFirstMove` walks back to the first move from the start state — this is the direction surfaced to the player via `GameplayManager.GetHint()` / `onShowHint`.

### UI/View layer (`UI/`)

Built on `Zillneuron.UILayout`'s `ViewContext` (singleton) and `ACompleteView` (base class for full-screen views: `MainMenuView`, `LevelsView`, `LoadingView`, `GameplayView`). Views call `ChangeActivityToNext<T>()` to navigate and `ViewContext.Instance.OpenDialog<T>(parameters)`/`HideDialog<T>()` for modal dialogs (`SettingsDialog`, `ShopDialog`, `GameOverDialog`, `ResultDialog`, `MenuDialog`), each with a corresponding `*Parameters` class passed in. `SetTexts()` overrides pull localized strings via `LocalizationManager`/`PhraseCollection`.

### Level data and persistence (`Game/Data/DataReader.cs`, `Game/Data/`)

- `GameData` — per-level DTO: `Id`, `StageId`, `GameId`, `MinimumStepsCount`, `Red`/`Blue`/`Green` start positions, `RedTarget`/`BlueTarget`/`GreenTarget`, optional `Mobiles`/`Statics` (`List<Vector2Int>`). `IsDoubleGame` is derived (`Green == GreenTarget`).
- `GameDataDynamic` — per-level progress (play count, best steps/coins), persisted via `PlayerPrefs` (`DataReader.SetGameDataDynamic`/`GetGameDataDynamic`) under `GameStartData.GameDataPrefsKey`.
- `DataReader.GetNextGameId()` / `DataManagementService.Key_NextGameId` — tracks which level the player is on next, also synced to Unity Cloud Save via `DataManagementService`.
- `GameStartData.CreateInstance()` loads all `GamesCount` (105) levels' `GameData`/`GameDataDynamic` up front at scene start.
