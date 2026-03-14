# Profile Combat Stats Placeholders

## Current state

Profile UI now includes placeholder rows for planned combat stats. These rows are visible in `ProfileOverviewView` but currently show a placeholder value (`profile_stat_not_implemented`) until combat systems provide real data.

### Placeholder rows added

- `profile_physical_damage_title`
- `profile_magic_damage_title`
- `profile_physical_resistance_title`
- `profile_magic_resistance_title`
- `profile_attack_speed_title`
- `profile_crit_chance_title`
- `profile_crit_multiplier_title`
- `profile_evasion_chance_title`
- `profile_hit_chance_title`

Localization keys are defined in `Assets/GameData/Localization/CSV/ui_common.csv`.

## Where placeholders are wired

- UI rendering: `Assets/Scripts/UI/Game/ProfileOverviewView.cs`
- Auto row template mapping: `AutoTemplateSpecs` in the same file.
- Placeholder localization key: `profile_stat_not_implemented`.

## Future implementation plan

1. Add runtime combat stat source
- Introduce a read model (for example `PlayerCombatDerivedStats`) that exposes current derived values:
  - physical damage
  - magic damage
  - physical resistance
  - magic resistance
  - attack speed
  - crit chance
  - crit multiplier
  - evasion chance
  - hit chance

2. Replace placeholders in profile view
- Update `ProfileOverviewView.Refresh(...)` to map each row to real values from the runtime model.
- Keep placeholder fallback when model is unavailable.

3. Optional persistence strategy
- If achievements need historical maxima or cumulative stats, extend `SaveData.AchievementStats` with explicit fields.
- If only current derived values are needed for display, do not persist them in save; compute at runtime.

4. Formatting standards
- Percent stats should use a percent format (for example `12.5%`).
- Multiplier stats should use `x` format (for example `1.75x`).
- Flat stats should use integer/decimal format depending on gameplay precision.

## Notes for continuation

- UI layout already supports auto-created rows from template and localization key injection into `LocalizedGlobalComponent` on `content_title`.
- `content_value` is written directly by `ProfileOverviewView`.
- If adding more stats, extend:
  - `ProfileRowId`
  - `AutoTemplateSpecs`
  - row binding fields (optional if only auto-template mode is used)
  - localization keys in `ui_common.csv`
