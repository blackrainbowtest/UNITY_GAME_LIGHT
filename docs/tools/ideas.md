# TODO — Base / Shelter System

## Today

### Base visuals
- [ ] Add base background
- [ ] Base is non-interactive (visual only, no gameplay interaction yet)

---

### Rest system (Bed interaction)

#### Core mechanics
- [ ] Rest action available on base
- [ ] Rest skips time depending on selected duration
- [ ] Rest restores character stats based on bed level

#### Time selection
- [ ] Time selection via slider
- [ ] Slider step: **15 minutes**
- [ ] Minimum rest time: **2 hours**
- [ ] Maximum rest time: **24 hours**
- [ ] Player can manually set rest duration

---

### Bed interaction UI

#### Bed click behavior
- [ ] Clicking the bed opens a modal window
- [ ] Modal buttons:
  - [ ] **Rest**
  - [ ] **Upgrade**
  - [ ] **Back** (closes modal)

---

### Bed upgrade system

#### Upgrade modal
- [ ] Upgrade opens a separate modal window
- [ ] Resources displayed as a table

#### Resource table columns
- [ ] Resource icon + name
- [ ] Required amount
- [ ] Amount in inventory
- [ ] Amount in storage
- [ ] Result status:
  - [ ] ✔️ icon if resources are sufficient
  - [ ] Red number showing missing amount if insufficient

#### Resource priority
- [ ] Clicking a table row sets priority
- [ ] Player chooses which inventory/storage is used first

---

## City interaction

### Building info
- [ ] Long press on city buildings
- [ ] Shows info modal:
  - Name
  - Description
  - Available actions (if any)

---
## Core concepts

### Enums & keys
- [ ] `DungeonLevelEnum`
- [ ] `DungeonLocationEnum`
- [ ] `DungeonObjectEnum`
- [ ] `ResourceEnum`
- [ ] `CurrencyEnum` (Gold / Crystals / DarkParticles)
- [ ] `PlayerRankEnum`
- [ ] `EnemyEnum`
- [ ] `BuffEnum`
- [ ] `DebuffEnum`

Enums используются:
- для ассетов
- для квестов
- для обучения
- для визуальных подсказок (пульсирующий круг)

---

## Dungeon access flow

### Dungeon scene
- [ ] Clicking dungeon entrance loads **Dungeon Scene**
- [ ] Scene shows dungeon levels as selectable nodes

### Dungeon levels
| Level | Player rank |
|------|------------|
| 0 | Non-adventurer (surroundings) |
| 1 | Rank E–F |
| 2 | Rank C–D |
| 3 | Rank B |
| 4 | Rank A |
| 5 | Rank S |
| Ultimate | Bosses (Rank SSS–S) |

---

### Initial state
- [ ] Player starts **without rank**
- [ ] Only **Level 1** is available
- [ ] Entry cost:
  - Gold: `0`
  - Crystals: `0`
  - Dark Particles: `0`

---

### Locked levels
- [ ] Locked levels are visually crossed with a chain
- [ ] Center shows a locked gate / lock icon

#### Hold interaction
- [ ] Holding on locked level opens info modal:
  - Required player rank
  - Entry requirements

#### Click interaction
- [ ] Clicking locked level:
  - Guard animation (hand stop gesture)
  - Modal message: access denied

---

## Dungeon level selection

### Accessible level click
- [ ] Opens **Entry Points modal**

### Entry points (Level 1 example)
- [ ] Flower meadow
- [ ] Wheat field
- [ ] Forest outskirts
- [ ] Forest
- [ ] Dark forest

Each location:
- has its own `DungeonLocationEnum`
- is linked to its parent `DungeonLevelEnum`

---

## Quest & guidance system

- [ ] Quests reference:
  - Dungeon level enum
  - Dungeon location enum
- [ ] Required location highlighted by:
  - Pulsating pink circle (not outline)
- [ ] Same system reused for tutorial guidance

---

## Dungeon data asset

### DungeonLevelData (Asset)
- [ ] Dungeon level enum
- [ ] Required player rank
- [ ] Entry cost (currencies)
- [ ] Available locations list

---

### DungeonLocationData (Asset)
- [ ] Location enum
- [ ] Linked dungeon level
- [ ] Possible backgrounds (list)
- [ ] Optional battle music override

---

## Enemy spawn system

### Spawn rules (per rank)
For each player rank:
- [ ] Enemy enum list
- [ ] Spawn chance per enemy
- [ ] Enemy level range (min / max)
- [ ] HP range
- [ ] Other base stats ranges

All values randomized **100% inside defined ranges**

---

### Buffs / debuffs
- [ ] Permanent buffs for enemies
- [ ] Permanent debuffs for enemies
- [ ] Global buffs for player
- [ ] Global debuffs for player
- [ ] Duration in turns (configurable)

---

## Battle start

- [ ] Background selected from location data
- [ ] If location has battle music:
  - It overrides default combat music
- [ ] Otherwise use default combat music

---

## Notes / Future hooks
- Bed level affects:
  - Rest efficiency
  - Time skip multiplier
  - Stat recovery amount
- UI should be reusable for other upgradeable base objects
- Dungeon system must be fully data-driven
- No hardcoded logic by level or rank
- Same enums reused by:
  - Quests
  - Tutorials
  - UI highlights
  - World events