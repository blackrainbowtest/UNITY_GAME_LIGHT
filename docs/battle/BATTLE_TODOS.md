> **Game Version:** 0.0.344

# Battle TODOs (UI + Flow)

## 1) Result modal wiring (scene)
- Open the battle scene (e.g. `FightScene`).
- Create a UI panel (modal) in the center of the screen.
- Add `BattleResultModalController` component to it.
- In Inspector, assign:
  - `root` (optional, if you want to hide/show a child root)
  - `titleText` (TMP_Text)
  - `rewardsText` (TMP_Text)
  - `okButton` (UnityEngine.UI.Button)
- In `BattleController` (in the battle scene), assign reference:
  - `resultModal` в†’ your modal controller

## 2) Configure tutorial exit
- In `BattleController` Inspector:
  - `tutorialReturnSceneName` should be `StartCityScene` (default).

## 3) Make battle entry set return scene (non-tutorial)
When entering the battle from any non-intro scene:
- Before loading `FightScene`, call:
  - `BattleExitContext.Set(new BattleExitData(SceneManager.GetActiveScene().name))`
- Also set battle mode:
  - `BattleEntryContext.Set(BattleMode.Normal)` (or another mode)

## 4) Rewards (later)
- Decide where rewards come from:
  - Enemy drops in `EnemyData` (recommended)
  - Or a separate drop table / resolver
- Fill `BattleResultData` in `BattleController.FinishBattle(...)`:
  - `goldGained`
  - `itemIds`

## 5) After-battle persistence (later)
- Apply rewards to `GameState.Instance.CurrentSave`:
  - Add gold to inventory
  - Add items
  - Save to slot if needed

## 6) UI action menus (later)
- Replace `OnAttackPressed()` в†’ show categories/menu:
  - Use `CombatActionRegistry.GetByCategory(...)`
  - UI sends `CombatActionId` back to controller

---

Notes:
- `BattleController` orchestrates only.
- `BattleCombatEngine` has pure logic.
- Result modal is UI-only and calls back to controller on OK.

