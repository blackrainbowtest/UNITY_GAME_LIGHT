# UI API Reference / Справочник UI API

See also:
- `BATTLE_API_REFERENCE.md` — battle architecture, contexts, actions, enemy AI/difficulty.

## ConfirmDialog

**RU:**
Модальное окно подтверждения с локализацией вопроса и коллбэками для кнопок "Да" и "Нет".

**EN:**
Modal confirmation dialog with localization key for the question and callbacks for "Yes" and "No" buttons.

## ConfirmDialog

**Описание:**
Модальное окно подтверждения с локализацией вопроса и коллбэками для кнопок "Да" и "Нет".


**Вызов / Usage:**
```
ConfirmDialog.Show(string questionKey, Action onYes, Action onNo = null)
```
- `questionKey` — ключ локализации для вопроса (например, "confirm_overwrite_save").
- `onYes` — действие при нажатии "Да". / callback for "Yes"
- `onNo` — действие при нажатии "Нет" (опционально). / callback for "No" (optional)


**Поведение / Behavior:**
- Создаёт экземпляр префаба ConfirmDialogModal. / Instantiates ConfirmDialogModal prefab.
- Подставляет локализованный текст в вопрос через LocalizedTextSetter/LocalizedTextComponent. / Sets localized text via LocalizedTextSetter/LocalizedTextComponent.
- После нажатия любой кнопки диалог уничтожается автоматически. / Dialog is destroyed after any button is pressed.


**Пример / Example:**
```
ConfirmDialog.Show(
    "confirm_delete_save",
    onYes: () => { /* удалить сохранение / delete save */ },
    onNo: () => { /* ничего не делать / do nothing */ }
);
```

---


## SaveLoadModalController

**RU:**
Контроллер окна выбора и управления сохранениями. Использует ConfirmDialog для подтверждения перезаписи или удаления слота.

**EN:**
Controller for save/load menu. Uses ConfirmDialog to confirm overwrite or delete slot.

**Основные методы / Main methods:**
- `OnSlotPressed(int slotId)` — обработка нажатия на слот (вызывает ConfirmDialog при перезаписи). / Handles slot press (calls ConfirmDialog for overwrite)
- `OnSlotLongPressed(int slotId)` — обработка долгого нажатия (вызывает ConfirmDialog для удаления). / Handles long press (calls ConfirmDialog for delete)

**Пример / Example:**
```
ConfirmDialog.Show(
    "confirm_overwrite_save",
    onYes: () => { /* перезаписать слот / overwrite slot */ },
    onNo: null
);
```

---


## Локализация UI / UI Localization

**RU:**
- `LocalizedTextSetter` — компонент для установки ключа локализации (свойство `key`, метод `UpdateText()`).
- `LocalizedTextComponent` — альтернативный компонент (свойство `textKey`, метод `UpdateText()`).

**EN:**
- `LocalizedTextSetter` — component for setting localization key (`key` property, `UpdateText()` method).
- `LocalizedTextComponent` — alternative component (`textKey` property, `UpdateText()` method).

**Паттерн использования / Usage pattern:**
```
var setter = textObject.GetComponent<LocalizedTextSetter>();
if (setter != null) {
    setter.key = "some_key";
    setter.UpdateText();
}
```

---

## Long Press & Tap API

### LongPressHandler
Reusable logic-only class for tracking long press (hold) gestures. UI-agnostic.

**Constructor:**
- `LongPressHandler(float duration)`

**Events:**
- `event Action OnStarted` — called when press starts
- `event Action<float> OnProgress` — called with progress (0..1) while holding
- `event Action OnCompleted` — called when hold is completed
- `event Action OnCanceled` — called if hold is interrupted

**Methods:**
- `void StartPress()`
- `void CancelPress()`
- `void Update(float deltaTime)`
- `void Reset()`

---

### LongPressProgressView
Visual-only component for showing a circular progress indicator under the finger/cursor.

**Serialized fields:**
- `Image progressImage`
- `RectTransform rootRectTransform`

**Methods:**
- `void Show(Vector2 screenPosition)` — show and move the circle
- `void SetProgress(float progress)` — update fill (0..1)
- `void Hide()` — hide the circle

---

### SaveSlotView (long press/tap logic)
- `event Action<int> PrimaryClicked` — fires on tap (short press)
- `event Action<int> LongPressed` — fires on long press complete
- `void SetProgressView(LongPressProgressView view)`
- `void ResetLongPressFlag()`
- Handles tap/hold logic: tap triggers click only if duration < progressShowDelay and not long press; otherwise, no click.

---

## AudioManager API

**RU:**
Глобальный менеджер звука и музыки (Singleton, DontDestroyOnLoad). Управляет музыкой, SFX и UI-звуками через AudioMixer и публичные методы.

**EN:**
Global sound and music manager (Singleton, DontDestroyOnLoad). Controls music, SFX, and UI sounds via AudioMixer and public methods.

### Основные методы / Main Methods

```csharp
// Получить экземпляр / Get instance
var audio = AudioManager.Instance;

// Воспроизвести музыку / Play music
audio.PlayMusic(audioClip); // audioClip — AudioClip (например, из ScriptableObject)

// Остановить музыку / Stop music
audio.StopMusic();

// Изменить громкость музыки / Set music volume
audio.SetMusicVolume(0.5f); // 0.0 ... 1.0

// Воспроизвести SFX / Play SFX
audio.PlaySfx(sfxClip);

// Изменить громкость SFX / Set SFX volume
audio.SetSfxVolume(0.8f);

// Воспроизвести UI-клик / Play UI click
audio.PlayUiClick();

// Изменить громкость UI / Set UI volume
audio.SetUiVolume(1.0f);
```

### Пример интеграции с локацией / Example with location ScriptableObject

```csharp
// location — объект BattleLocationData с полем AudioClip music
if (location.music != null)
    AudioManager.Instance.PlayMusic(location.music);
```

**Все методы доступны из любого кода через AudioManager.Instance. Singleton создаётся автоматически и не уничтожается между сценами.**
