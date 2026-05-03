> **Game Version:** 0.0.345

# Visuals & Animations

## IDs
- `BattleVisualAnimId` — Assets/Scripts/Battle/BattleVisual/BattleVisualAnimId.cs
  - Включает hit-анимации: `Hit` и `LustHit`.

## Character view
- `BattleCharacterView` — Assets/Scripts/Battle/BattleVisual/BattleCharacterView.cs
  - Проигрывает idle и one-shot анимации по `BattleVisualAnimId`.

### Idle: Default + Ambient
Текущая логика idle:
- Первая idle в списке — **default loop**.
- Остальные — **ambient** (редкие одноразовые анимации).
- Попытка сыграть ambient — примерно раз в `ambientIdleIntervalSeconds` (по умолчанию ~4s).
- Выбор ambient идёт случайно, но без повторов “по кругу” (shuffle-bag).

## Outfit visuals
- `OutfitVisuals` — Assets/Scripts/Battle/BattleVisual/OutfitVisuals.cs
  - Хранит визуальные настройки конкретного outfit.

### Hit timing (удар по кадру)
`OutfitVisuals` содержит маппинг таймингов удара:
- `hitTimings`: `attackAnimId -> hitAtFrame + useLustHit`

Поведение:
- `hitAtFrame >= 0` — hit-анимация цели запускается, когда атакующий достигает этого кадра.
- `hitAtFrame == -1` — hit-анимация цели **игнорируется** (не проигрывается).
- `useLustHit == true` — вместо `Hit` используется `LustHit`.

### Variations
- Для `LustHit` можно задать вариации (`lustHitVariations`), чтобы `LustHit01/02/...` выбирались рандомно на базе данных outfit.

## Где запускается hit
- `BattleController` (оркестрация) запускает hit-анимацию цели в момент, вычисленный из `hitAtFrame` и FPS клипа атакующего.
- Тип hit выбирается по фактическому изменению ресурсов (HP vs LP) между `before/after` состояниями.
