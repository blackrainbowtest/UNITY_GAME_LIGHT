> **Game Version:** 0.0.198

# Data & Import

## EnemyData
- `EnemyData` — Assets/Scripts/Battle/BattleData/EnemyData.cs
  - ScriptableObject с данными врага.
  - Содержит и **max**, и **стартовые текущие** значения (`hp/mp/sp/lp`, `maxHp/maxMp/maxSp/maxLp`).
  - Также содержит визуальные поля (`outfitId`, `visualProfile`, `idleAnimation`) и AI-конфиг (`allowedActions`).

## Контентная база
- `BattleContentDatabase` — Assets/Scripts/Battle/BattleData/BattleContentDatabase.cs
  - Каталог доступных врагов и локаций.

## Импорт из JSON (Editor)
- `EnemyDataImporter` — Assets/Editor/EnemyDataImporter.cs
  - Source JSON по умолчанию: `Assets/GameData/enemies.json`
  - Output folder по умолчанию: `Assets/GameData/Battle/Data/Enemies`

После правок JSON нужно переимпортировать врагов:
- Unity menu: `Tools → Import EnemyData from JSON`
