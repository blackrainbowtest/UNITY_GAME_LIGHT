> **Game Version:** 0.0.345

# Audio System (AudioCue + SceneMusicConfig)

Этот документ описывает текущую аудио-систему UDA2: музыка по сценам, UI-клики и игровые звуки через **AudioCue**.

---

## Быстрый старт (3 минуты)

1) Создай AudioCue:
- Project → Create → **_Audio → Audio Cue**
- Заполни: `Category`, `Clip` (и при желании `DefaultVolume`, `PitchRange`)

2) Музыка по сценам:
- Создай `SceneMusicConfig` (Create → **_Audio → Scene Music Config**)
- Добавь entry: `sceneName` (ровно имя сцены) → `musicCue`
- Назначь `sceneMusicConfig` в `AudioManager`

3) UI click:
- Создай `AudioCue` с `Category = Ui`
- Назначь его в `AudioManager.uiClickCue`

---

## Категории (dropdown)

<details>
<summary><b>Music</b> — фоновая музыка (микшер: Music)</summary>

**Когда использовать:** музыка сцен, меню, интро.

**Как играет:** через `AudioManager.PlayMusic(AudioClip)` (внутри — `musicSource` и fade-in).

**Что настроить в Unity:**
- В `AudioManager` назначить `musicSource` и `musicGroup` (AudioMixerGroup).
- В `SceneMusicConfig` указать `musicCue` для нужных сцен.

<details>
<summary><b>API (коротко)</b></summary>

- `AudioManager.Instance.SetNextSceneMusic(AudioCue cue)` — принудительно задать музыку для следующей загружаемой сцены.
- `SceneMusicConfig` — связывает `sceneName -> musicCue`.
</details>

<details>
<summary><b>Подробнее</b></summary>

- Музыка выбирается при `SceneManager.sceneLoaded`:
  - если заранее задан `SetNextSceneMusic(cue)` → он имеет приоритет;
  - иначе берётся из `SceneMusicConfig` по имени сцены;
  - иначе `StopMusic()` (тишина).
</details>

</details>

<details>
<summary><b>Ui</b> — звуки интерфейса (микшер: SFX, по вашей логике UI)</summary>

**Когда использовать:** кнопки, клики, hover, подтверждения.

**Как играет:** через `AudioManager.Play(AudioCue)` → внутри вызывается `uiSource.PlayOneShot(...)`.

**Что настроить в Unity:**
- Назначить `uiSource` + `uiGroup`.
- Для клика: назначить `AudioManager.uiClickCue`.

<details>
<summary><b>API (коротко)</b></summary>

- `AudioManager.Instance.PlayUiClick()` — проиграть `uiClickCue`.
- `AudioManager.Instance.Play(AudioCue cue)` — универсальный вызов.
</details>

<details>
<summary><b>Подробнее</b></summary>

- Почему `PlayOneShot`: UI-звуки часто наслаиваются (быстрые клики), и `PlayOneShot` не ломает текущий `AudioSource.clip`.
- Pitch берётся из `cue.PitchRange`.
</details>

</details>

<details>
<summary><b>Sound</b> — игровые звуки (микшер: Sound, по вашей логике gameplay)</summary>

**Когда использовать:** атаки, попадания, шаги, зелья.

**Как играет:** через `AudioManager.Play(AudioCue)` → уходит в `PlaySfx(...)` (пул AudioSource).

**Что настроить в Unity:**
- Назначить `sfxPrefab` (AudioSource prefab), `sfxGroup`, `sfxPoolSize`.

<details>
<summary><b>API (коротко)</b></summary>

- `AudioManager.Instance.Play(attackCue)` — проиграть звук атаки.
</details>

<details>
<summary><b>Подробнее</b></summary>

- Для тайминга удара лучше вызывать звук там, где фиксируется попадание/урон, либо через Animation Event.
- Для частых звуков используется пул `AudioSource`, чтобы не плодить компоненты.
</details>

</details>

<details>
<summary><b>Sfx</b> — legacy/совместимость</summary>

Сейчас `AudioCategory.Sfx` трактуется как игровая категория (как `Sound`) и проигрывается через пул.

Рекомендуемый путь: постепенно переводить новые игровые звуки на `Sound`, а старые оставить как есть.

</details>

---

## Подкатегории (организация ассетов)

Подкатегории — это просто папки. Unity не ограничивает расположение `AudioCue`.

Рекомендованная структура:
- `Assets/GameData/Audio/Cue/Music/`
- `Assets/GameData/Audio/Cue/UI/`
- `Assets/GameData/Audio/Cue/Sound/`

Пример именования:
- `s_mainMenu` (scene music)
- `ui_btn_click`
- `snd_attack_swing_01`

---

## Примеры

### 1) Проиграть звук атаки из компонента

```csharp
using UnityEngine;
using UDA2.Audio;

public sealed class AttackSfx : MonoBehaviour
{
    [SerializeField] private AudioCue attackCue;

    public void PlayAttackSfx()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.Play(attackCue);
    }
}
```

### 2) Музыка сцены через SceneMusicConfig

- `SceneMusicConfig.entries`:
  - `sceneName = "MainMenuScene"`
  - `musicCue = s_mainMenu`

---

## Частые проблемы

<details>
<summary><b>Ничего не слышно</b></summary>

- Нет `AudioListener` в сцене.
- В `AudioManager` не назначены `musicSource` / `uiSource`.
- В `AudioCue` не назначен `Clip`.
- Громкость в настройках = 0 (микшер уходит в -80 dB).
</details>

<details>
<summary><b>MissingReferenceException на AudioSource</b></summary>

Если `AudioManager` живёт через `DontDestroyOnLoad`, а его `AudioSource`/`sfxParent` были объектами сцены, они могут уничтожаться при смене сцен. В `AudioManager` есть защита, которая перепривязывает их под себя и пересоздаёт пул при необходимости.
</details>


