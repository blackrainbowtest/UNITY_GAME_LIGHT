# Guild Runtime API

Документ описывает backend API гильдии для интеграции UI/вьюшек и будущей системы уведомлений.

## Что уже реализовано

- Ежедневный refresh доски квестов в 12:00 игрового времени.
- No-repeat цикл квестов до исчерпания пула.
- Сохранение списков квестов:
  - `activeQuestIds` — квесты на доске,
  - `selectedQuestIds` — принятые игроком,
  - `completedQuestIds` — завершённые,
  - `failedQuestIds` — проваленные/отменённые.
- API уведомлений на ключах локализации (без UI-системы уведомлений).

## Основные классы

- `Assets/Scripts/SaveSystem/Guild/GuildRuntimeAPI.cs`
- `Assets/Scripts/SaveSystem/Guild/GuildNotificationAPI.cs`
- `Assets/Scripts/SaveSystem/Guild/GuildService.cs`
- `Assets/Scripts/_Core/GameTime/GuildTimeSyncBridge.cs`

## Инициализация

`GuildTimeSyncBridge` создаётся автоматически при старте и подписывается на `GameTimeService.TimeChanged`.

При каждом изменении времени вызывается:

- `GuildRuntimeAPI.HandleTimeChanged(day, minuteOfDay)`

Если произошёл refresh доски, отправляется событие:

- `GuildNotificationAPI.NotificationRequested("ui_guild_quest_board_refreshed")`

## Публичное API для UI

### Конфигурация (опционально)

```csharp
GuildRuntimeAPI.Configure(rankConfigAsset, boardConfigAsset);
```

Если `Configure` не вызван, API попробует загрузить ассеты из Resources:

- `Resources/Config/Guild/GuildRankProgressionConfig`
- `Resources/Config/Guild/GuildQuestBoardConfig`

### Квесты

```csharp
GuildRuntimeAPI.TrySelectQuest(questId);
GuildRuntimeAPI.TryCancelQuest(questId);
GuildRuntimeAPI.TrySubmitQuest(questId, out var questDef);
```

События уведомлений при успехе:

- select: `ui_guild_quest_selected`
- cancel: `ui_guild_quest_cancelled`
- submit: `ui_guild_quest_completed`

### Ап ранга

```csharp
GuildRuntimeAPI.CanRankUp(out var requirement);
GuildRuntimeAPI.TryRankUp(out var newRank);
```

Событие уведомления при успешном апе:

- `ui_guild_rank_up`

## Подписка на уведомления (future-ready)

```csharp
private void OnEnable()
{
    GuildNotificationAPI.NotificationRequested += HandleGuildNotification;
}

private void OnDisable()
{
    GuildNotificationAPI.NotificationRequested -= HandleGuildNotification;
}

private void HandleGuildNotification(string localizationKey)
{
    // Сейчас: можно логировать или прокинуть в временный HUD.
    // Будущая система уведомлений: покажет локализованный toast по ключу.
    Debug.Log($"Guild notification key: {localizationKey}");
}
```

## Рекомендации перед стартом визуала

- Добавить в таблицу локализации ключи:
  - `ui_guild_quest_board_refreshed`
  - `ui_guild_quest_selected`
  - `ui_guild_quest_cancelled`
  - `ui_guild_quest_completed`
  - `ui_guild_rank_up`
- Убедиться, что rank/board config ассеты существуют и корректно заполнены.
- В UI использовать только `GuildRuntimeAPI`, а не прямой доступ к `GuildService`.

## Режим `frame + content` для hotspot (новый)

Для сцены гильдии можно использовать общий каркас окна (close/header/background) и отдельные контент-префабы.

### Что изменено

- `LocationPrefabHotspot` теперь поддерживает два режима:
  - legacy: только `contentPrefab`;
  - новый: `framePrefab + contentPrefab`.
- Добавлен компонент `LocationWindowFrame`:
  - `contentRoot` — куда вставлять контент внутри каркаса;
  - `closeButton` — общая кнопка закрытия окна (логика закрытия контенту не нужна).

### Как настроить в Unity

1. Создай префаб каркаса окна (например, `GuildWindowFrame`).
2. На root каркаса повесь `LocationWindowFrame`.
3. Укажи в `LocationWindowFrame`:
   - `contentRoot` (RectTransform-контейнер тела окна),
   - `closeButton` (кнопка закрытия из каркаса).
4. На интерактивном объекте с `LocationPrefabHotspot`:
   - `framePrefab` = `GuildWindowFrame`,
   - `contentPrefab` = нужный контент (например, доска квестов или окно ранга).
5. На root контент-префаба добавь `LocationWindowContentMeta`:
  - `titleLocalizationKey` (рекомендуется) или `titleText`,
  - при необходимости `contentTitleText` (если контент тоже должен показывать этот заголовок внутри себя).

При открытии hotspot заголовок каркаса берётся из `LocationWindowContentMeta` автоматически.
Если на `headerTitleText` в каркасе висит `LocalizedGlobalComponent`, то в него передаётся именно `titleLocalizationKey`.

### Результат

- При клике hotspot создаётся каркас, контент вставляется внутрь `contentRoot`.
- Кнопка `closeButton` из каркаса закрывает всё окно целиком.
- Пока каркас открыт, глобальный UI сцены временно скрывается; при закрытии автоматически восстанавливается.
- Старые hotspot'ы, где задан только `contentPrefab`, продолжают работать как раньше.
