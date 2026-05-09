> **Game Version:** 0.0.354

# Enemy AI

- `EnemyActionSelector` — Assets/Scripts/Battle/Combat/EnemyAI/EnemyActionSelector.cs

## Входные данные
`SelectEnemyAction(difficulty, enemy, registry, state, rng)`:
- Берёт `enemy.allowedActions` (если пусто — fallback на `FastAttack/NormalAttack/HeavyAttack`).
- Отфильтровывает недоступные по ресурсам/условиям.

## Спец-кейсы
- `Block` выбирается только если:
  - у врага сейчас нет block-armor
  - HP врага достаточно низкое (ниже ~45%)
  - действие доступно по ресурсам
- `CounterAttack` выбирается только если враг действительно **блокировал** на прошлом ходе игрока.

## Сложность
- `Easy`: 80% выбирает “weak” (минимальный primary value), 20% — “strong” (максимальный).
- `Normal`: 60% weak, 40% strong.
- `Hard`:
  1) Если есть летальная атака (`HpDamage >= PlayerHp`) — выбирает летальную с минимальной относительной стоимостью.
  2) Иначе максимизирует `score = primaryValue / (0.10 + relativeCost)`.

Где:
- `primaryValue` = `HpDamage` (если > 0), иначе `HpHealSelf`.
- `relativeCost` = `MpCost/maxMp + SpCost/maxSp + LpCost/maxLp` (нормализация по максимальным пулам врага).
