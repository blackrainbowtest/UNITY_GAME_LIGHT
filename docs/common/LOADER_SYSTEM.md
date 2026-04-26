# Loader System (SceneFlow + LoadingScreen)

## Purpose
This loader pipeline provides consistent scene transitions with:
- A dedicated loading scene.
- Real progress from Unity async loading.
- Optional fake progress envelope for better UX.
- Safety gates for scene readiness, music readiness, and deferred localization warmup.

## Main Components
- `Assets/Scripts/SceneFlow/SceneFlowManager.cs`
- `Assets/Scripts/UI/LoadingScreenController.cs`
- `Assets/Scripts/UI/Game/GlobalUISceneBinder.cs`
- `Assets/Scripts/Localization/LocalizationLoadGate.cs`

## High-Level Flow
1. Gameplay code calls `SceneFlowManager.LoadScene(target, data, minLoadingTime)`.
2. Manager routes transition through `LoadingScene`.
3. `LoadingScreenController` registers itself as `ILoadingScreen`.
4. Transition state is set to `in progress` only after loading scene is active.
5. Manager executes staged loading:
   - optional fake pre-envelope (`0 -> ~10-15%`)
   - real async streaming (`~start -> 90%`)
   - optional min loading time smoothing
   - activation phase
   - optional scene load task queue
   - optional scene-ready wait
   - optional music-ready wait
   - optional fake finalize envelope (`~90-95% -> 100%`)
6. Loader hides and transition state is cleared.

## Battle Fast Path
For battle entry transitions, the project uses `SceneTransitionData` flags to reduce perceived latency:
- `SkipSceneLoadTasks = true`
- `SkipSceneReadyWait = true`
- `SkipMusicWait = true`
- `DisableFakeProgressEnvelope = true`
- `minLoadingTime = 0f`

This makes battle transitions favor responsiveness over cinematic progress animation.

## Why UI No Longer Blanks Before Loader
`GlobalUISceneBinder` hides global UI during transition.
To avoid a blank gap, `SceneFlowManager` now raises transition state only after `LoadingScene` is active (or when already inside it).
This guarantees that UI is not hidden too early.

## Safety/Resilience
- If `LoadingScene` async load fails, manager falls back to direct target load routine.
- If no loading screen instance is found in time, the transition still proceeds.
- Scene task execution has per-task timeout guard.
- Deferred localization queue can be drained in batches after activation.

## Tuning Knobs (Inspector)
`SceneFlowManager` exposes practical tuning fields:
- Scene ready timeout
- Music wait timeout
- Fake envelope ranges and durations
- Task queue progress ranges and task timeout
- Loading screen resolve timeout
- Deferred localization drain batch size and timeout

`LoadingScreenController` exposes:
- Progress smoothing speed
- Background rotation source
- Tooltip source and rotation interval
- Manual tooltip navigation

## User-Facing Behavior Summary
On normal transitions, players see a smooth staged progress with polished start/end.
On battle transitions, players get the fastest possible handoff to gameplay with minimal artificial delay.
