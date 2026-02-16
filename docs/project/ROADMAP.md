> **Game Version:** 0.0.198

Обратный поток из подземелья в полнолуние квест по защите города

```cs
Items dictionary template
{
  "id": "item.example",
  "type": "Generic",

  "meta": {
    "nameKey": "",
    "descriptionKey": "",
    "icon": "",
    "rarity": "Common",
    "stackable": true,
    "maxStack": 1
  },

  "economy": {
    "value": 0,
	"canSale": true
  },

  "usage": {
    "useType": null,
    "consumable": false,
    "cooldown": 0
  },

  "effects": {
    "hp": 0,
    "mp": 0,
    "sp": 0,
    "lp": 0,
    "statuses": []
  },

  "equipment": {
    "slot": null,
    "stats": {}
  },

  "combat": {
    "damage": null,
    "range": null,
    "speed": null,
    "tags": []
  },

  "world": {
    "canSpawnInCHests": true,
    "canDropedInEnemy": true,
  },

  "flags": [
    "Currency",
    "QuestItem"
  ]
}
```

## Инвентарь
- Представляет из себя грид сетку. Сетка содержит ячейки. Для пустых ячеек есть свой спрайт по умолчанию. Каждая ячейка может содержать одновременно один тип предметов. Кол-во предметов в ячейке определяется `item.meta.maxStack`. Ячейка отображает кол-во предметов в ней снизу слева, `item.meta.icon`. Клик на ячейку открывает контекстное меню рядом с ячейкой (по умолчанию слева снизу но если контекстное меню не помешается на экране можно отразить x или y как это делается например в windows). Контекстное меню содержит (Использовать/Надеть смотря это экипировка, расходник либо нету такого раздела если это например ресурс), переместить в хранилище/пересестить в инвентарь (будет доступно если я в убежише там смогу открыть хранилище), переместить (переместить на другой слот кликаем на нужный если слот пустой перемешается если там был такой же предмет добавляется к нему по допустимое кол-во `item.meta.maxStack` остаток остается на том же месте, если другой предмет то меняет их местами) \


# Inventory System — Mobile First (v1)

## Overview
The inventory is a **mobile-first grid-based system**, optimized for touch interaction.
All actions are performed via **tap and context menus**.
Drag & Drop is intentionally **not used**.

The inventory UI is presentation-only and does not contain gameplay logic.

---

## 1. Grid & Slots

### Grid
- Two-dimensional grid
- Adaptive size depending on screen resolution
- Slot size:
  - Always square
  - Scales via Canvas Scaler

### Slot (Inventory Slot)
Each slot can be:
- Empty
- Occupied by **one item type**

Slot stores:
- `item` reference
- `count`

### Visuals
- Empty slot:
  - Default empty slot sprite
- Occupied slot:
  - `item.meta.icon`
  - Item count:
    - Bottom-left corner
    - Hidden if `item.meta.maxStack == 1`

---

## 2. Item Stacking

- Only one item type per slot
- Maximum stack size: item.meta.maxStack
- If `maxStack == 1`:
- Item is non-stackable

---

## 3. Touch Interaction

### Single Tap on Slot
- Opens **context menu** for that slot

### Tap on Empty Slot
- No action

---

## 4. Context Menu

### General Rules
- Appears near the selected slot
- Default position: bottom-left of slot
- Automatically flips on X and/or Y axis if it would go off-screen
- Closed by:
- Tapping outside
- System back button

### Menu Content (Dynamic)
Menu options depend on item type and game state.

#### Usage Actions
- `Use` — consumable items
- `Equip` — equipment items
- Not shown — resources, currency, non-usable items

#### Container Actions
- `Move to Storage`
- `Move to Inventory`
(Visible only when storage is available, e.g. shelter)

#### Slot Movement
- `Move`
- Activates **Move Mode**

---

## 5. Move Mode (No Drag & Drop)

### Activation
- Triggered via `Move` in context menu

### Behavior
- Available target slots are visually highlighted
- Player taps target slot

### Move Resolution
- Target slot empty:
- Item moves to target
- Target slot contains same item:
- Stack is merged up to `maxStack`
- Remainder stays in original slot
- Target slot contains different item:
- Items swap places

Move Mode ends automatically after action.

---

## 6. Containers

### Inventory & Storage
- Inventory and storage are separate containers
- Both:
- Use the same grid logic
- Use the same slot rules
- Difference:
- Data source
- Availability (depends on location/game state)

---

## 7. Mobile UX Requirements

- Large tap areas (slot, not icon-only)
- Context menu buttons:
- Minimum 44–48 dp height
- Clear visual feedback:
- Tap animation
- Stack merge animation
- Optional haptic feedback

---

## 8. Optional Mobile Enhancements (v2)

### Long Tap
- Long tap on slot:
- Opens detailed item info

### Quick Use
- Double tap:
- Instantly uses item
- Only for consumables

### Item Lock
- Lock flag on item:
- Prevents accidental use or movement
- Recommended for equipment and quest items

---

## 9. Explicitly Out of Scope

- Drag & Drop
- Hover-based interactions
- Mouse-specific UX
- Slot resizing per item

---

## 10. Architectural Notes

- Inventory system:
- Does NOT decide what actions are allowed
- Does NOT apply item effects
- Responsibilities:
- Inventory: state + rendering
- Context Menu: available actions
- Game Logic: validation and execution

Inventory is a **data-driven UI**, not a gameplay system.

---

## UI States

- `Idle`
- `ContextMenuOpen`
- `MoveMode`

State transitions must be explicit and mutually exclusive.

# ------------------------------------------------------------

======================================================================\
create checkbox turn off/on NSFW (it will be turn off NSFW content show an actions like lust attacks and special actions after 50% 75% 100% lust)
no NSFW content on fights
need to add API to check isNSFWOn(); true/false
======================================================================\
skip already watched option buttons to skip / ask on repeating / dont skip
======================================================================\
Убежище можно улучшать кровать, костер, верстак, алхимическая станция, хранилище
для улучшений используется ресурсы которые добиваются в квестах и от мобов

ветки
полено
доска
камень
глина
волокно
веревка
гвозди



выделения гоблина
тестикулы гоблина
уши гоблина
мясо гоблина
выделения слизи
ядро слизи
плазма слизи
выделения волка
шкура волка
клыки волка
мясо волка


======================================================================\

