# UI API Reference / Справочник UI API

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

## Примечания
- Все диалоги и окна уничтожаются через Destroy(gameObject) после завершения работы.
- Для корректной работы ConfirmDialogModal префаб должен содержать компонент ConfirmDialog и подключённые поля TMP_Text, Button и локализационные компоненты.
