# Battle Damage Model Roadmap

## Current state

Battle HP damage uses a unified formula in runtime and tooltip preview:

- `finalHpDamage = round(baseAttack * hpDamageMultiplier)`
- `baseAttack` is selected by action category:
  - attack/utility/seduction actions use `physicalDamage`
  - magic actions use `magicDamage`
- Formula implementation: `Assets/Scripts/Battle/Combat/CombatDamageModel.cs`
- Runtime usage: `Assets/Scripts/Battle/Combat/BattleCombatEngine.cs`
- Tooltip usage: `Assets/Scripts/Battle/BattleUI/BattleHUDController.cs`

This guarantees tooltip values and actual applied damage are derived from the same code path.

## Current damage source

Player damage stats are now read from save in `BattleSceneEntryPoint`:

- `stats.physicalDamage` (fallback to legacy `stats.damage`)
- `stats.magicDamage` (fallback to physical/default)

Resolver extension points:

- `ResolvePlayerPhysicalDamage(...)`
- `ResolvePlayerMagicDamage(...)`

Enemy base attack uses `EnemyData.attack`.

## Why this structure

We keep central resolvers (`ResolvePlayerPhysicalDamage`, `ResolvePlayerMagicDamage`) and one central formula (`CombatDamageModel`) so future systems can be connected without touching action resolution flow:

- no duplicated formulas in UI/runtime
- no large rewrites in `BattleController` / `BattleCombatEngine`
- minimal regression risk when adding equipment/perks/status modifiers

## Planned integration order

1. Equipment bonuses
- Read weapon/offhand/accessory contributions in `ResolvePlayerPhysicalDamage` / `ResolvePlayerMagicDamage`.
- Decide stacking rule:
  - additive baseline (recommended first)
  - optional multiplicative layers after additive phase

2. Character progression bonuses
- Add rank/perk/talent bonuses in the same resolver or in a dedicated `PlayerCombatDerivedStats` service.

3. Temporary combat modifiers
- Add pre-battle and in-battle modifier sources:
  - buffs/debuffs
  - weather/location effects
  - encounter-specific modifiers

4. Optional split by damage schools
- If needed later, extend formula to separate channels:
  - physical base attack
  - magic power
  - corresponding resistances/penetration

## Suggested future API shape

When systems become available, prefer a small read model over ad-hoc calculations spread across classes.

Example target structure:

- `PlayerCombatDerivedStats`
  - `BaseAttack`
  - `PhysicalPower`
  - `MagicPower`
  - `PhysicalResistance`
  - `MagicResistance`
  - `CritChance`
  - `CritMultiplier`

- `IPlayerCombatStatsProvider`
  - `PlayerCombatDerivedStats Build(SaveData save, BattleContext context)`

Then:

- `BattleSceneEntryPoint` requests derived stats once on battle start
- `BattleController` passes values to engine/tooltip context
- `ProfileOverviewView` can display the same derived stats source

## Non-goals for current phase

- No resistance/crit formula implementation yet.
- No runtime mutation pipeline for weather/buffs yet.

## Checklist when starting equipment integration

- Add equipment attack fields (or mapping) in inventory/equipment runtime model.
- Implement resolver logic in `ResolvePlayerPhysicalDamage` / `ResolvePlayerMagicDamage` (or move to provider service).
- Add tests for:
  - zero/negative values clamped correctly
  - tooltip equals runtime damage for each action
  - dark spell heal preview follows computed damage
- Update `docs/battle/Combat_and_Actions.md` with finalized formula and order of operations.
