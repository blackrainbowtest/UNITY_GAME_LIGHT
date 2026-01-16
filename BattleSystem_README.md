# Battle System Integration Guide

## Архитектура входа в бой

1. **Перед загрузкой боевой сцены**
   - Вызовите `BattleEntryContext.Set(BattleMode mode)` для передачи режима боя (например, Tutorial, Normal и т.д.).
   - Убедитесь, что в `GameState.Instance.CurrentSave` актуальные данные игрока (hp, hpMax).

2. **BattleSceneEntryPoint**
   - На боевой сцене должен быть компонент `BattleSceneEntryPoint`.
   - В инспекторе назначьте:
     - `EnemySpawnTable` — таблица врагов для рандомного выбора.
     - `BattleLocationData` — данные локации (если используются).
     - `BattleController` — явная ссылка на контроллер боя на сцене.

3. **Логика старта боя**
   - При старте сцены `BattleSceneEntryPoint`:
     1. Получает режим боя из `BattleEntryContext.Consume()`.
     2. Собирает `PlayerCombatSnapshot` из `GameState.Instance.CurrentSave.player.stats`.
     3. Случайно выбирает врага через `EnemySpawnResolver.Resolve(enemyTable)`.
     4. Собирает `BattleContext` и вызывает `battleController.StartBattle(context)`.

4. **API игрока**
   - Данные берутся из:
     - `GameState.Instance.CurrentSave.player.stats.hp` — текущее HP
     - `GameState.Instance.CurrentSave.player.stats.hpMax` — максимальное HP
   - Для создания снапшота: `new PlayerCombatSnapshot(maxHp, currentHp)`

5. **API врага**
   - Враг выбирается из `EnemySpawnTable` через `EnemySpawnResolver`.
   - Данные врага (`EnemyData`):
     - `enemyName`, `icon`, `maxHp`, `attack` и др.

6. **BattleController**
   - Не инициирует бой сам!
   - Получает готовый `BattleContext` и запускает бой через `StartBattle(context)`.

---

## Пример кода старта боя

```csharp
// Перед загрузкой сцены:
BattleEntryContext.Set(BattleMode.Normal);
SceneFlowManager.Instance.LoadScene("FightScene");

// На сцене:
// BattleSceneEntryPoint всё сделает автоматически
```

---

## Важно
- Все зависимости назначаются через инспектор.
- Нет статиков и God Object — только явные точки входа и передачи данных.
- Для расширения (катсцены, туториал, разные режимы) меняйте только входные данные, архитектура не ломается.

---

## Troubleshooting
- Если бой не стартует — проверьте, что все поля в инспекторе назначены и данные игрока актуальны.
- Для тестов можно временно задать значения по умолчанию в BuildPlayerSnapshot.

---

> Документ поддерживает актуальную архитектуру на январь 2026. Для изменений обновляйте этот файл.
