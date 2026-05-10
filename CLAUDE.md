# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**淮畔科创行** — Unity 2022.3.62f3c1 educational game about ecological protection, green manufacturing, and regional innovation along the Huaihe River. Chinese-language product, 1920x1080, targeting low-end devices (30 FPS, lowest quality).

## Build & Run

Open the project in Unity 2022.3.62f3c1. No CLI build scripts exist. Run by opening `Assets/Scenes/BaseScene.unity` and pressing Play. The BaseScene bootstrap auto-loads StartupScene additively.

Unity MCP (via `mcp.json` at project root) connects to `http://127.0.0.1:8080` for editor automation.

## Architecture

### Three-Layer Assembly Structure

Strict dependency direction enforced by `.asmdef` files:

```
Presentation/  →  Logic/  →  Core/
```

- **Core** (`Assets/Core/`) — Zero dependencies. EventBus, data models (`SaveDataModels.cs`), UI builder, file I/O (`FileUtils.cs`), dialogue data (`StoryScenarioLibrary.cs`).
- **Logic** (`Assets/Logic/`) — Singletons: `GameManager` (state machine), `AccountManager` (3 save slots), `TransitionManager` (scene routing). `StoryProgressService` for chapter completion and report generation.
- **Presentation** (`Assets/Presentation/`) — UI components, chapter handlers, mini-games. Never calls Logic singletons directly; uses EventBus.
- **Bootstrap** (`Assets/Bootstrap/`) — Not a separate assembly. Each scene has a bootstrap script that programmatically constructs ALL UI at runtime. No prefabs, no scene-stored UI.

### Scene Architecture

BaseScene is persistent (`DontDestroyOnLoad`). All other scenes load **additively** on top via `TransitionManager`. Navigation is entirely EventBus-driven:

```
UI click → EventBus.Trigger(new IntentEvent(...)) → TransitionManager loads next scene
```

`TransitionManager` handles fade-in → unload old scene → additive load → fade-out.

### Key Patterns

- **EventBus decoupling**: UI never references Logic directly. `IntentEvent` types defined in `Core/UIEvents.cs`. Game events (mini-game start/complete/fail) also go through EventBus.
- **Programmatic UI**: All UI created via `UIBuilder` static methods (CreateCanvas, CreateText, CreateTextButton, CreateInputField). Reference resolution 1920x1080.
- **Coroutine ticket system**: `DialogueUI` uses `_typingTicket` version counters to cancel stale typing coroutines without `StopCoroutine`. `HideAllUI()` increments the ticket.
- **Data isolation**: Per-account JSON files at `Application.persistentDataPath/Saves/Slot_N.json`. `AccountManager.SwitchAccount()` performs full cleanup (save → unload → null → GC).

### Chapter Flow

Each chapter: dialogue prelude → guide → mini-game (independent scene) → post-game dialogue → knowledge cards → transition. `Chapter1Handler` drives `DialogueUI` through a state machine (states 0-5). `StoryScenarioLibrary` holds all dialogue tree data. Chapters 2/3 not yet implemented.

## Key Files

| File | Purpose |
|------|---------|
| `Assets/Core/EventBus.cs` | Static pub/sub event bus |
| `Assets/Core/UIEvents.cs` | IntentEvent and IntentType enum |
| `Assets/Core/UIBuilder.cs` | Runtime UI factory |
| `Assets/Core/StoryScenarioLibrary.cs` | All dialogue trees and mini-game configs |
| `Assets/Core/SaveDataModels.cs` | AccountData, ChapterProgress, ChapterIds |
| `Assets/Logic/TransitionManager.cs` | Scene routing with fade transitions |
| `Assets/Logic/GameManager.cs` | Global state machine singleton |
| `Assets/Logic/AccountManager.cs` | Save slot management |
| `Assets/Logic/StoryProgressService.cs` | Chapter completion, report generation |
| `Assets/Bootstrap/Chapter1SceneBootstrap.cs` | Chapter 1 UI construction |
| `Assets/Presentation/UI/DialogueUI.cs` | Reusable dialogue system |
| `Assets/Presentation/Chapter/Chapter1Handler.cs` | Chapter 1 flow controller |
| `Assets/Presentation/MiniGame/MiniGameBase.cs` | Abstract mini-game base class |
| `大纲/plan.md` | Full 8-phase development plan |
