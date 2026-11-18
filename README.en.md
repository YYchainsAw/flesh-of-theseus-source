# Flesh of Theseus

[简体中文](README.md) · [Play on itch.io](https://yychainsaw.itch.io/flesh-of-theseus)

[![Flesh of Theseus cover](https://img.itch.zone/aW1nLzMyMDQ4MTY5LnBuZw==/508x254%23b/zp%2B%2BHa.png)](https://yychainsaw.itch.io/flesh-of-theseus)

Flesh of Theseus is a 2D point-and-click puzzle game made for Xueyuan Road Game Jam 2026. Players awaken in a sealed space, search for missing body parts, and swap limbs to gain abilities, solve puzzles, and reach different endings.

## My role

I’m YYchainsAw. I worked on programming and project management. This repository showcases the game’s core state model, command execution, rule interfaces, and EditMode tests.

## Code walkthrough

| Start here | What it covers |
| --- | --- |
| [GameSession.cs](src/CollisionJam.Core/GameSession.cs) | Handles pickup, equipment, puzzle, and navigation commands. Each accepted change updates the state before publishing a notification. |
| [GameState.cs](src/CollisionJam.Core/GameState.cs) and [IGameStateView.cs](src/CollisionJam.Core/IGameStateView.cs) | Track inventory, equipped body parts, the current view, and puzzle progress. Rules and UI can query that state through a read-only interface. |
| [Rule contracts](src/CollisionJam.Core/Contracts) | Define how the game session consults the rules for equipment, pickups, navigation, and puzzles. |
| [EditMode tests](tests/CollisionJam.Core.EditModeTests) | Cover rejected commands, single notifications after state changes, repeated commands, reentrant callbacks, and snapshots of puzzle code input. |

When a player equips a limb, `TryEquip` validates the item and slot, then calls `CheckEquip`. If the rule approves the change, `GameSession` updates the slot and raises `StateChanged`.

## Gameplay preview

[![Gameplay from Flesh of Theseus](https://img.itch.zone/aW1nLzMyMDk1NDQ3LnBuZw==/original/xiuDOY.png)](https://yychainsaw.itch.io/flesh-of-theseus)

## Development environment

The original game was developed with Unity `2022.3.62f3c1`. The [core source](src/CollisionJam.Core) is an engine-independent C# assembly. The [test assembly](tests/CollisionJam.Core.EditModeTests) uses NUnit and Unity Test Framework. To run the tests in Unity, place both directories and their `.asmdef` files under `Assets/` in the same Unity project with Unity Test Framework installed, then run the EditMode suite in Test Runner.

## Copyright and permitted use

The source code is published for portfolio review. Copyright remains with the respective rights holders. You may view and fork this repository under GitHub’s terms. Copying, modifying, redistributing, or using the code commercially beyond what those terms and applicable law permit requires prior permission from the relevant rights holders.

The game’s art, audio, and production scenes remain in the original project. Copyright remains with the respective rights holders.

## Team

- YYchainsAw · Programming and project management
- FUSI14 · Game design
- Coldmushroom · Art
