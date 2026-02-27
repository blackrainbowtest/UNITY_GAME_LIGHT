# BattleController Refactor Guidelines

## Purpose

This document defines architectural rules and refactoring strategy for splitting `BattleController` into smaller, maintainable systems.

The goal is to:
- Reduce responsibility overload
- Improve testability
- Prevent future architectural collapse
- Make the battle system extensible (statuses, multi-hit, interrupts, new mechanics)

This document must be followed by both AI tools and human contributors.

---

# 1. Architectural Principles (Non-Negotiable)

## 1.1 Single Responsibility

Each class must have one reason to change.

If a class:
- manages turn flow
- calculates mechanics
- plays animations
- spawns projectiles
- manages UI
- calculates escape chance

→ it is doing too much.

---

## 1.2 Controller Must Orchestrate, Not Implement

`BattleController` must:
- control flow
- coordinate systems
- manage high-level battle state

It must NOT:
- instantiate projectiles
- calculate detailed escape formulas
- manage hit timing internals
- read outfit pixel offsets
- resolve animation frame logic

---

## 1.3 Combat Logic and Visual Logic Must Be Separated

Game mechanics (HP, LP, SP, block, status) must not depend on animation timing.

Visual systems may react to combat results, but combat resolution must not depend on visuals.

---

# 2. Current Problem Overview

`BattleController` currently handles:

- Turn phase management
- Coroutines
- Escape logic
- Projectile spawning
- Animation sync
- Status clearing
- UI interaction
- Modal instantiation
- End-of-battle rules
- LP threshold logic

This creates:

- Tight coupling
- High regression risk
- Poor testability
- Hard extension for future mechanics

---

# 3. Target Architecture

## 3.1 BattleController (Orchestrator Only)

Responsibilities:

- Manage battle lifecycle
- Switch turn phases
- Call systems
- Trigger visual execution
- Decide when battle ends

It should look like:

```csharp
ResolveAction();
yield return visualExecutor.PlayAction(actionId, actor, target);
ApplyState();
if (endSystem.TryResolve(combatState))
    FinishBattle();
turnSystem.SwitchTurn();
```

Nothing more.

---

## 3.2 BattleTurnSystem

Handles:

- TurnPhase enum
- Phase transitions
- Validation of allowed actions

Removes manual phase switching from controller.

---

## 3.3 BattleVisualExecutor

Handles:

- Playing attacker animation
- Playing target hit animation
- Synchronizing impact
- Handling projectile spawning

Moves all logic from:

`PlayActionWithTargetHitAndWait`

Controller should call:

```csharp
yield return visualExecutor.PlayAction(actionId, actorView, targetView, beforeState, afterState);
```

---

## 3.4 BattleEscapeSystem

Handles:

- Escape chance calculation
- Success/failure determination
- Escape penalties
- Escape LP logic

Moves:

- CalculateEscapeChance01
- Escape sequences
- Escape penalties

Controller should call:

```csharp
if (escapeSystem.TryEscape(combatState, context, out var result))
{
    FinishBattle(result);
}
```

---

## 3.5 BattleEndConditionSystem

Handles:

- HP death
- LP threshold victory
- Simultaneous LP fill logic

Moves:

`TryFinishByLpThreshold`

Controller should call:

```csharp
if (endSystem.TryResolve(combatState, lastAction, out var finishData))
{
    FinishBattle(finishData);
}
```

---

## 3.6 ProjectileSpawner

Handles:

- spawn offset conversion
- direction logic
- projectile instantiation
- impact callback

Controller must never convert pixel offsets.

---

# 4. Refactor Strategy (Step-By-Step)

Refactor must be incremental.

Do NOT:
- rewrite everything
- break working battle logic

---

## Step 1 — Extract EscapeSystem

Move:
- CalculateEscapeChance01
- Escape success routine
- Escape fail routine
- LP penalty logic

Inject:
- CombatState
- BattleContext
- PlayerView
- HUD reference

Test escape independently.

---

## Step 2 — Extract VisualExecutor

Move:
- PlayActionWithTargetHitAndWait
- PlayActionVisualAndWait
- Hit timing logic
- Projectile logic
- One-shot event wiring

Controller only waits for executor result.

---

## Step 3 — Extract EndConditionSystem

Move:
- LP threshold logic
- HP death checks

Controller reacts only to result object.

---

## Step 4 — Extract TurnSystem

Replace:

```csharp
turnPhase = TurnPhase.PlayerTurn;
```

With:

```csharp
turnSystem.BeginPlayerTurn();
```

Remove direct enum writes from controller.

---

# 5. Rules for AI Assistants

When generating new battle features:

AI must NOT:
- Add new mechanics inside BattleController
- Add projectile math to controller
- Add animation frame logic to controller

AI must:
- Propose a new system class if responsibility grows
- Keep controller under 800 lines
- Keep methods under 80 lines
- Respect separation of concerns

If unsure → ask before modifying architecture.

---

# 6. Rules for Human Contributors

Before adding new feature:

Ask:

1. Is this combat logic?
2. Is this visual logic?
3. Is this flow logic?
4. Is this UI logic?

Place feature accordingly.

Never place new gameplay systems directly inside `BattleController`.

---

# 7. Future-Proofing Goals

Architecture must support:

- Multi-hit attacks
- Interrupts
- Damage reflection
- New resource types
- Combo systems
- Advanced AI logic
- Networked battle (future possibility)

Without heavy modification of `BattleController`.

---

# 8. Red Flags

If any of these happen, refactor immediately:

- Controller exceeds 1500 lines
- More than 3 nested coroutines inside controller
- Controller directly instantiates gameplay prefabs
- Controller reads visual pixel offsets
- Adding new status requires editing controller

---

# 9. Definition of Done

Refactor complete when:

- Controller only orchestrates
- Visual system isolated
- Escape isolated
- End conditions isolated
- Turn system isolated
- Combat logic remains pure

---

Design for expansion.

A modular battle system scales.
A monolithic one collapses.