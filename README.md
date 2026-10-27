# Yahtzee — F20SC Coursework 1

A simplified Yahtzee implementation in C# WinForms, targeting .NET 10 on Windows.

## Build

    dotnet restore
    dotnet build

## Run

    dotnet run

## Publish standalone executable

    dotnet publish -c Release -r win-x64 --self-contained true \
      -p:PublishSingleFile=true \
      -p:IncludeNativeLibrariesForSelfExtract=true \
      -p:PublishTrimmed=false

Output: `bin/Release/net10.0-windows/win-x64/publish/Yahtzee.exe`

## Data files

- `yahtzee.db` — SQLite database of completed games, created next to the executable on first run
- `*.yz` — save-game files (JSON), created via `Game → Save Game`

## Project structure

| Folder | Responsibility |
|---|---|
| `Models/` | Domain types: `Die`, `DiceSet`, `Player`, `ScoreCard`, `Game` |
| `Logic/` | `ScoreCalculator` (pure scoring) and `GameManager` (orchestration) |
| `Saving/` | `SaveGameManager` — JSON serialization for save/load |
| `Database/` | `GameDatabase` — SQLite persistence for high scores |
| `AI/` | `ComputerPlayer` — computer opponent |
| `UI/` | WinForms presentation: `MainForm`, `NewGameDialog`, `Program` |

## Rules

Implementation follows the simplified Yahtzee rules in Appendix B of the
coursework spec: 13 rounds, 3 rolls per round, 13 scoring categories,
no upper-section bonus, no Yahtzee bonus.