> **Game Version:** 0.0.194

# Battle API Reference / Справочник Battle API

Этот документ раньше был монолитной справкой. Сейчас он **разбит на несколько страниц**.

Начни отсюда:
- [Battle Docs Index](INDEX.md)

Ключевые страницы:
- [Architecture & Flow](Architecture_and_Flow.md)
- [Combat & Actions](Combat_and_Actions.md)
- [Visuals & Animations](Visuals_and_Animations.md)
- [Enemy AI](Enemy_AI.md)
- [End-of-round](End_of_Round.md)
- [Data & Import](Data_and_Import.md)

## Примечания
- Страницы в battle-разделе стараются ссылаться на **реальные пути** в `Assets/Scripts/...`.
- Seduction-атаки и “эмоциональный урон” уже реализованы через `LpDamage` (см. Combat & Actions).
- JSON для врагов импортируется editor-утилитой из `Assets/GameData/enemies.json` (см. Data & Import).


