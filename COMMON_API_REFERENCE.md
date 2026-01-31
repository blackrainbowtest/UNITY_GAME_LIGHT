
# Common API Reference

## Game Time (Day/Clock)

Game time is stored inside `SaveData.time`:
- `day` (1-based, starts at Day 1)
- `minuteOfDay` (0..1439)

### Runtime Service

The runtime time system is backed by:
- `UDA2.GameTime.GameTimeService` (auto-created, persists across scenes)
- `UDA2.GameTime.GameTimeAPI` (static gameplay-friendly API)

### Read

```csharp
using UDA2.GameTime;

int day = GameTimeAPI.Day;         // 1,2,3...
string time = GameTimeAPI.Time24h; // "08:05"
int hour = GameTimeAPI.Hour24;     // 0..23
int minute = GameTimeAPI.Minute;  // 0..59
```

### Add Time (Instant)

```csharp
using UDA2.GameTime;

// Example: action costs 30 minutes
GameTimeAPI.AddMinutes(30);

// Example: rest costs 2 hours
GameTimeAPI.AddHours(2);
```

### Add Time (Animated / Step-by-step)

This updates `SaveData.time` gradually and triggers UI updates per step.

```csharp
using UDA2.GameTime;

// Example: add 60 minutes with nice ticking
GameTimeAPI.AddMinutesAnimated(60);
```

You can also tune animation speed globally:

```csharp
using UDA2.GameTime;

// Every 0.02s add +2 minutes
GameTimeAPI.ConfigureAnimation(stepSeconds: 0.02f, minutesPerStep: 2);
```

### UI

Attach `UDA2.UI.Game.GameTimeHUDController` to a UI object.
Assign two `TMP_Text` references:
- Day value text (top-left number only; label like "Day" should be a separate localized TMP_Text)
- Time text (under the day)

