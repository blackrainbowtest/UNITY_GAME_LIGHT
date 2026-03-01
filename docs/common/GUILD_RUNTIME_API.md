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
- Регистрация в гильдии: фиксированный первый ап `None -> G` за `10` золота.

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
GuildRuntimeAPI.GetCurrentRank();
GuildRuntimeAPI.TryGetRankUpViewData(out var rankViewData);
```

Событие уведомления при успешном апе:

- `ui_guild_rank_up`

`TryGetRankUpViewData` возвращает данные для UI регистрации:

- текущий и целевой ранг,
- требования по золоту/уровню/квестам,
- список ресурсов с прогрессом:
  - `required`,
  - `inventoryOwned`,
  - `storageOwned`,
  - `totalOwned`,
  - `isMet`.

Важно: для апа ранга проверка и списание предметов выполняются по сумме инвентаря и хранилища.

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

## Registrar MVP (без финальных артов)

Для UI регистрации уже есть готовые скрипты:

- `Assets/Scripts/UI/Guild/GuildRegistrarWindowController.cs`
- `Assets/Scripts/UI/Guild/GuildRegistrarResourceRowView.cs`
- `Assets/Scripts/SaveSystem/Guild/GuildRankVisualConfigAsset.cs`

### Что создать в Unity

1. `GuildRankProgressionConfigAsset` (если ещё не создан):
  - заполни требования для рангов начиная с `F` и выше,
  - `G` не нужно задавать как первый шаг (он фиксирован через регистрацию).

2. `GuildRankVisualConfigAsset`:
  - добавь записи по рангам,
  - каждому рангу задай `textColor`,
  - `icon` можно оставить пустым до появления артов.

3. Префаб строки ресурса:
  - root с `GuildRegistrarResourceRowView`,
  - назначь поля: `iconImage`, `itemIdText`, `requiredText`, `currentText`.

4. Контент окна регистратора:
  - повесь `GuildRegistrarWindowController`,
  - назначь тексты/иконки ранга,
  - назначь `resourcesRoot` и `resourceRowPrefab`,
  - назначь кнопку `rankUpButton`,
  - назначь `rankVisualConfig`,
  - опционально `itemDatabase` для автопоказа иконок ресурсов.

### Какие данные получает UI

`GuildRuntimeAPI.TryGetRankUpViewData(out data)` возвращает:

- `currentRank`, `targetRank`,
- золото/уровень/квесты (текущее и требуемое),
- список ресурсов (`requiredItems`) для скролла,
- по каждому ресурсу:
  - `required`,
  - `inventoryOwned`,
  - `storageOwned`,
  - `totalOwned`,
  - `isMet`.

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
