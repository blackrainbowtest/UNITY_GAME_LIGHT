# Player & Enemy API Reference

## Player (PlayerCombatSnapshot)
- **Файл:** Assets/Game/Battle/Core/PlayerCombatSnapshot.cs
- **Назначение:** Immutable snapshot of player combat state at battle start.
- **Поля:**
  - `int maxHp { get; }` — максимальное HP игрока
  - `int currentHp { get; }` — текущее HP игрока
- **Конструктор:**
  - `PlayerCombatSnapshot(int maxHp, int currentHp)`

## Enemy (EnemyData)
- **Файл:** Assets/Game/Battle/Data/EnemyData.cs
- **Назначение:** ScriptableObject с базовыми данными врага
- **Поля:**
  - `string enemyName` — имя врага
  - `Sprite icon` — иконка врага
  - `int maxHp` — максимальное HP врага
  - `int attack` — сила атаки врага
- **Примечание:**
  - Нет поля для текущего HP (runtime-статус врага хранится отдельно)

---

## Пример доступа к полям

```csharp
// Player
int playerHp = context.Player.currentHp;
int playerHpMax = context.Player.maxHp;

// Enemy
int enemyHpMax = context.Enemy.maxHp;
string enemyName = context.Enemy.enemyName;
```

---

## Рекомендации
- Для текущего HP врага используйте отдельный runtime-объект или добавьте поле в EnemyData при необходимости.
- Все поля и методы указаны с учётом текущей архитектуры проекта.
