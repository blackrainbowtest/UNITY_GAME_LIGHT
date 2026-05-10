> **Game Version:** 0.0.355

# Player & Enemy API Reference

## Player (PlayerCombatSnapshot)
- **Файл:** Assets/Scripts/Battle/_Core/PlayerCombatSnapshot.cs
- **Назначение:** immutable snapshot состояния игрока на момент старта боя.
- **Поля (актуальные):**
  - `string OutfitId` — outfit для визуалов в бою
  - `int MaxHP`, `int CurrentHP`
  - `int MaxMP`, `int CurrentMP`
  - `int MaxSP`, `int CurrentSP`
  - `int MaxLP`, `int CurrentLP`
  - `int RegenHpPerTurn`, `int RegenMpPerTurn`, `int RegenSpPerTurn`
- **Конструктор:**
  - `PlayerCombatSnapshot(int maxHp, int currentHp, int maxMp = 0, int currentMp = 0, int maxSp = 0, int currentSp = 0, int maxLp = 0, int currentLp = 0, int regenHpPerTurn = 5, int regenMpPerTurn = 2, int regenSpPerTurn = 4, string outfitId = "outfit_01")`

## Enemy (EnemyData)
- **Файл:** Assets/Scripts/Battle/BattleData/EnemyData.cs
- **Назначение:** ScriptableObject с данными врага (включая стартовые значения ресурсов).
- **Поля (важное):**
  - `string id` — стабильный id (для сейвов/поиска)
  - `string enemyName`, `Sprite icon`
  - `string outfitId`, `CharacterVisualProfile visualProfile`, `IdleAnimation idleAnimation`
  - Статы (max + стартовые текущие):
    - `int hp/mp/sp/lp`
    - `int maxHp/maxMp/maxSp/maxLp`
  - `CombatActionId[] allowedActions`
  - Награды: `goldReward`, `expReward`, `lootTable`
  - Реген врага за ход: `regenHpPerTurn`, `regenMpPerTurn`, `regenSpPerTurn`

---

## Пример доступа к полям

```csharp
// Player
int playerHp = context.Player.CurrentHP;
int playerHpMax = context.Player.MaxHP;

// Enemy
int enemyHpMax = context.Enemy.maxHp;
string enemyName = context.Enemy.enemyName;
```

---

## Рекомендации
- Текущее состояние боя (включая текущие HP/LP в процессе боя) хранится в `CombatState`, а не в `EnemyData`.
- `EnemyData.hp/mp/sp/lp` — это стартовые значения на входе в бой (seed), а не “живое” runtime-состояние.


