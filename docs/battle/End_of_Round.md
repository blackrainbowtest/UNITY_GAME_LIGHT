> **Game Version:** 0.0.355

# End-of-round effects

В конце раунда (после хода врага) применяются эффекты “конца раунда” одним батчем.

## Где это живёт
- `BattleController.EndOfRound` — Assets/Scripts/Battle/_Core/BattleController.EndOfRound.cs

## Что входит сейчас
- Пассивная регенерация ресурсов.
- Очередь эффектов, которые можно накапливать в течение хода и применить одним итогом.

## Queue API
Методы:
- `BattleController.QueueEndOfRoundEffect(...)`
- `BattleController.ClearEndOfRoundEffects()`

Идея: разные системы (статусы/ауры/доты) добавляют эффекты в очередь, а в конце раунда они суммируются и применяются единым обновлением HUD.
