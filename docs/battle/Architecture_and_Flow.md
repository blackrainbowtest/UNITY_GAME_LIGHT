> **Game Version:** 0.0.354

# Architecture & Flow

## Цель слоёв
- **Combat engine** — чистая логика (без Unity/UI).
- **Controller** — оркестрация (жизненный цикл боя, вызовы engine, сцены/выход, визуалы).
- **UI** — “тонкий” слой: показывает панели и отправляет `CombatActionId`.

## Entry / Context
Точка входа сцены боя:
- `BattleSceneEntryPoint` — Assets/Scripts/Battle/BattleSceneEntryPoint.cs
  - Собирает данные (игрок/враг/локация/режим/сложность).
  - Учитывает debug-настройки и one-shot контексты.
  - Создаёт `BattleContext` и вызывает `BattleController.StartBattle(context)`.

Контейнер входных данных:
- `BattleContext` — Assets/Scripts/Battle/_Core/BattleContext.cs
  - `PlayerCombatSnapshot Player`
  - `EnemyData Enemy`
  - `BattleLocationData Location`
  - `BattleMode Mode`
  - `EnemyDifficulty EnemyDifficulty`

One-shot контексты (передача между сценами):
- `BattleEntryContext` — Assets/Scripts/Battle/_Core/BattleEntryContext.cs
- `BattleExitContext` — Assets/Scripts/Battle/_Core/BattleExitContext.cs
- `BattleEnemyContext` — Assets/Scripts/Battle/BattleEnemyContext.cs
- `BattleEnemyDifficultyContext` — Assets/Scripts/Battle/_Core/BattleEnemyDifficultyContext.cs

Восстановление pending battle из сейва:
- `BattleSaveBridge` — Assets/Scripts/Battle/_Core/BattleSaveBridge.cs

## Orchestration
- `BattleController` — Assets/Scripts/Battle/_Core/BattleController*.cs
  - Инициализирует registry/engine.
  - Принимает команды от UI (через интерфейс действий UI).
  - Прогоняет ход игрока → визуалы → ход врага → визуалы.
  - Завершает бой и инициирует выход из battle-сцены.

## Start / Exit flow (в общих чертах)
Старт:
1) (Опционально) выставить контексты (enemy/difficulty/return).
2) Загрузить battle-сцену.
3) `BattleSceneEntryPoint` создаёт `BattleContext`.
4) `BattleController.StartBattle(context)`.

Выход:
- `BattleController.FinishBattle(playerWon)` → `ExitBattle()`.
- Выбор return-сцены идёт через `BattleExitContext` (и fallback’и), дальше — через scene flow слой проекта.
