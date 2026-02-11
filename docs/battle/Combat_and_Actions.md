> **Game Version:** 0.0.194

# Combat & Actions

## Combat state
- `CombatState` — Assets/Scripts/Battle/Combat/CombatState.cs
  - Immutable state боёвки.
  - Хранит текущие ресурсы игрока и врага: `HP/MP/SP/LP`.
  - Также содержит флаги/поля для правил (например блок/броня блока).

## Combat engine
- `BattleCombatEngine` — Assets/Scripts/Battle/Combat/BattleCombatEngine.cs
  - Применяет действие к `CombatState` и возвращает результат (новый state + статус выполнения).
  - Engine не знает про Unity, анимации, сцены и UI.

## Actions registry
- `CombatActionId` — enum (см. Assets/Scripts/Battle/Combat/Actions/*)
- `CombatActionRegistry` — Assets/Scripts/Battle/Combat/Actions/CombatActionRegistry.cs
  - Единая таблица `CombatActionId -> CombatActionData`.

## Action data
- `CombatActionData` — Assets/Scripts/Battle/Combat/Actions/CombatActionData.cs
  - Примерные категории полей:
    - Урон/хил: `HpDamage`, `HpHealSelf`
    - Эмоциональный урон: `LpDamage` (увеличивает LP цели)
    - Стоимости: `MpCost`, `SpCost`, `LpCost`
    - Гейты: например `RequiresPlayerBlockedLastTurn` (используется для некоторых player-side правил)

### Emotional damage / LP
В проекте “атаки похоти” реализованы как **эмоциональный урон** через `LpDamage`:
- Если действие даёт `LpDamage > 0`, engine увеличивает LP цели.
- В ветке seduction (`SeductionAct1..Act4`) это и используется.

Дальше визуальный слой может отличать HP-урон от LP-урона, чтобы выбрать правильную hit-анимацию (см. страницу про visuals).
