# Battle API Reference / Справочник Battle API

Документация описывает текущую реализацию боя (Unity C#), чтобы быстро понимать:
- где какая ответственность
- как UI подключается к бою
- как стартует/заканчивается бой
- где описаны боевые действия (actions)
- как работает AI и сложность врага

## Принципы
- Single Source of Truth: боевое состояние хранится в `CombatState`.
- UI — "тупой": только переключает панели и отправляет команды (IDs), не содержит логики боя.
- Combat engine — чистая логика: не знает про Unity/сцены/UI.

---

## Архитектура (слои)

### 1) Scene Entry / Context
**Задача:** собрать входные данные и стартовать бой.

- `BattleSceneEntryPoint` (MonoBehaviour)
  - Стартует бой в battle-сцене.
  - В debug-режиме может подменить врага/статы игрока/return scene/сложность.
  - Создаёт `BattleContext` и вызывает `BattleController.StartBattle(context)`.

- `BattleContext` (plain C#)
  - Single Source of Truth для инициализации боя.
  - Содержит:
    - `PlayerCombatSnapshot Player`
    - `EnemyData Enemy`
    - `BattleLocationData Location`
    - `BattleMode Mode`
    - `EnemyDifficulty EnemyDifficulty`

- One-shot контексты для передачи параметров при загрузке battle-сцены:
  - `BattleEnemyContext` — какой враг выбран
  - `BattleExitContext` — куда возвращаться после боя
  - `BattleEnemyDifficultyContext` — сложность врага (easy/normal/hard)

---

### 2) Orchestration
**Задача:** управлять жизненным циклом боя и сценами (без логики урона/костов).

- `BattleController` (MonoBehaviour)
  - Запускает бой (`StartBattle`), инициализирует engine/registry.
  - Принимает команды от UI через `IBattleUIActions`.
  - После действия игрока запускает ход врага.
  - Завершает бой и выходит из battle-сцены.

---

### 3) Combat Logic
**Задача:** чистая логика изменений state.

- `CombatState` (immutable)
  - Текущие ресурсы игрока и врага (HP/MP/SP/LP).
  - Флаги, необходимые правилам (например `PlayerBlockedLastTurn`).

- `BattleCombatEngine`
  - `ResolvePlayerAction(state, actionData)`
  - `ResolveEnemyAction(state, actionData)`
  - Возвращает `CombatResolution`:
    - `CombatState State`
    - `CombatActionResult Result` (`Executed` / `Rejected_*`)

---

### 4) Actions Registry
**Задача:** единый список всех боевых действий и их параметров.

- `CombatActionId` (enum)
  - Стабильные ID, которыми пользуется UI и AI.

- `CombatActionRegistry`
  - Маппит `CombatActionId -> CombatActionData`.
  - Любая кнопка/UI/AI, которая отправляет actionId, требует записи в registry.

---

### 5) Enemy AI
**Задача:** выбрать, чем враг атакует.

- `EnemyActionSelector.SelectEnemyAction(difficulty, enemy, registry, state, rng)`

**Сложность:**
- `Easy`: 80% выбирает самую слабую доступную атаку, 20% — самую сильную.
- `Normal`: 60% слабую, 40% сильную.
- `Hard`: выбирает наиболее эффективную с учётом ресурсов.
  - Если есть летальная атака (`HpDamage >= PlayerHp`) — добивает.
  - Иначе максимизирует эффективность: `damage / (0.10 + relativeCost)`
  - `relativeCost = mpCost/maxMp + spCost/maxSp + lpCost/maxLp`.

---

## Passive Regen (per turn)
Пассивное восстановление применяется **в начале хода** стороны:
- начало хода игрока: +HP/+MP/+SP из `PlayerCombatSnapshot` (LP не регенится)
- начало хода врага: +HP/+MP/+SP из `EnemyData` (LP не регенится)

Текущие дефолты для игрока (если нигде не переопределять):
- `RegenHpPerTurn = 5`
- `RegenMpPerTurn = 2`
- `RegenSpPerTurn = 4`

Для врагов значения задаются в `Assets/Data/enemies.json` и импортируются в EnemyData ассеты.

---

## UI → Battle API

### `IBattleUIActions`
UI вызывает методы интерфейса, не трогая `BattleController` напрямую (кроме биндинга).

Критичный метод:
- `OnCombatActionSelected(CombatActionId actionId)`

Также (в зависимости от UI):
- `OnRunPressed()`
- `OnSurrenderPressed()`
- `OnSkipTurnPressed()`

### `BattleHUDController`
- Переключает панели (root/submenus).
- В обработчиках кнопок вызывает `actions.OnCombatActionSelected(...)`.

### `StatBarView` (values + deltas)
`StatBarView` умеет (если назначить ссылки в инспекторе):
- показывать числовое значение `current/max` (например `45/100`)
- показывать всплывающий дельта-текст при изменении значения (например `+5` или `-10`)

HUD сам считает дельты по предыдущему `BattleHUDState` и вызывает `ShowDelta(...)` при каждом обновлении.

---

## Start/Exit Flow

### Старт
1) Выставить контексты (опционально):
   - `BattleEnemyContext.Set(enemyData)`
   - `BattleEnemyDifficultyContext.Set(EnemyDifficulty.Hard)`
   - `BattleExitContext.SetReturnToScene(sceneName)`
2) Загрузить battle-сцену.
3) `BattleSceneEntryPoint` создаёт `BattleContext`.
4) `BattleController.StartBattle(context)`.

### Завершение
- `BattleController.FinishBattle(playerWon)` показывает результат (если есть модалка) и вызывает `ExitBattle()`.
- `ExitBattle()` использует `BattleExitContext` и fallback'и (save/default) и грузит сцену через `SceneFlowManager` если он доступен.

---

## Enemies JSON schema (MVP)
Файл: `Assets/Data/enemies.json`

### Поля (текущие)
- `enemyName` (string)
- `iconPath` (string)
- `hp/mp/sp/lp` (int) — текущие значения на старте
- `maxHp/maxMp/maxSp/maxLp` (int)
- `attack` (int) — пока запасное поле (не участвует в engine напрямую)
- `regenHpPerTurn/regenMpPerTurn/regenSpPerTurn` (int) — пассивная регенерация за ход врага (LP не регенится)
- `allowedActions` (string[]) — **опционально**, список действий из `CombatActionId`

### Примеры
```json
{
  "enemyName": "Goblin",
  "iconPath": "Assets/Art/Icons/goblin.png",
  "hp": 50,
  "mp": 15,
  "sp": 10,
  "lp": 5,
  "maxHp": 50,
  "maxMp": 15,
  "maxSp": 10,
  "maxLp": 20,
  "attack": 8,
  "allowedActions": ["FastAttack", "NormalAttack", "HeavyAttack", "FireSpell"]
}
```

**Важно:** после правки JSON нужно переимпортировать врагов:
- Unity Menu: `Tools → Import EnemyData from JSON`

---

## Что ещё НЕ сделано (чтобы понимать текущую стадию)
- Статусы (яд/оглушение/баффы), длительность эффектов.
- Событийная лента (combat events) для анимаций.
- Реальные механики seduction/utility (пока placeholder).
- Учет защиты/уклонения/крита.
- Инвентарь в бою.

## Следующие расширения (планируемые)
- Добавить в enemy JSON: `affixes/effects` (например poisonOnHit: { turns: 2, chance: 0.25 }).
- Добавить `CombatEvent[]` в `CombatResolution` (damage, status applied, etc.) для анимаций.
- Добавить правила выбора целей/дебаффов в `Hard` AI.
