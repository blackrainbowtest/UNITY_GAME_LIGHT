//   ____  ____  _     ___ _____ ____  _   _    _    ____  ____    ____ _____ _   _ ____ ___ ___  
//  / ___||  _ \| |   |_ _|_   _/ ___|| | | |  / \  |  _ \|  _ \  / ___|_   _| | | |  _ \_ _/ _ \ 
//  \___ \| |_) | |    | |  | | \___ \| |_| | / _ \ | |_) | | | | \___ \ | | | | | | | | | | | | |
//   ___) |  __/| |___ | |  | |  ___) |  _  |/ ___ \|  _ <| |_| |  ___) || | | |_| | |_| | | |_| |
//  |____/|_|   |_____|___| |_| |____/|_| |_/_/   \_\_| \_\____/  |____/ |_|  \___/|____/___\___/ 
//
/* ******************************************************************************************************** */
/*                                                                                                          */
/*   File: Assets\Scripts\Battle\_Core\BattleController.ActionEffects.cs                                    */
/*                                                        /\_/\                                             */
/*                                                       ( •.• )                                            */
/*   By: unluckydungeonadventure.gmail.com                > ^ <                                             */
/*                                                                                                          */
/*   Created: 2026/01/23 01:39:28 by UDA                                                                    */
/*   Updated: 2026/01/23 01:39:28 by UDA                                                                    */
/*                                                                                                          */
/* ******************************************************************************************************** */

using Game.Battle.Combat.Actions;
using Game.Battle.Statuses;

namespace Game.Battle
{
    public partial class BattleController
    {
    private const int FireSpellBurningTurns = 2;
    private const int BlockTurns = 2;

        // Future hook: data-driven mapping from actions/weapons to statuses.
        // Keep this hook isolated so BattleController doesn't grow again.
        //
        // -----------------------
        // EXAMPLE (simple rule)
        // -----------------------
        // Goal: FireSpell should apply Burning for 2 turns to the target.
        //
        // switch (actionId)
        // {
        //     case CombatActionId.FireSpell:
        //     {
        //         if (actorIsPlayer)
        //             AddOrRefreshEnemyStatus(StatusEffectId.Burning, turns: 2);
        //         else
        //             AddOrRefreshPlayerStatus(StatusEffectId.Burning, turns: 2);
        //         break;
        //     }
        // }
        //
        // NOTE: With the CURRENT implementation, statuses are unique by Id.
        // If you hit with FireSpell twice подряд, we do NOT create 2 icons/effects.
        // We refresh the same status to the max turnsLeft:
        // - existing Burning(1) + new Burning(2) => Burning(2)
        // - existing Burning(3) + new Burning(2) => Burning(3) (because existing is stronger/longer)
        // This behavior is implemented in AddOrRefreshStatus(...): it uses Mathf.Max(existingTurns, newTurns).
        //
        // -----------------------------------------------
        // EXAMPLE ("replace only if new is stronger")
        // -----------------------------------------------
        // Sometimes "stronger" != "longer".
        // Example: Burning has (turnsLeft, damagePerTurn). If you cast a weaker fire spell,
        // it should NOT overwrite a stronger burning already on target.
        //
        // For that you need to store potency/strength in StatusInstance.
        // One option:
        //   struct StatusInstance { StatusEffectId Id; int TurnsLeft; int Power; }
        //
        // Then refresh logic becomes:
        //   if (existing.Power > newPower)
        //       keep existing (optionally refresh turns to max)
        //   else if (existing.Power < newPower)
        //       overwrite power AND set turns (or max)
        //   else
        //       same power -> just refresh turns (max)
        //
        // Pseudocode:
        //   var newTurns = 2;
        //   var newPower = 5; // e.g. 5 damage/turn
        //   var existing = FindStatus(targetList, StatusEffectId.Burning);
        //   if (existing == null) Add(Burning(newTurns, newPower));
        //   else
        //   {
        //       if (existing.Power > newPower)
        //           existing.TurnsLeft = Mathf.Max(existing.TurnsLeft, newTurns);
        //       else
        //       {
        //           existing.Power = newPower;
        //           existing.TurnsLeft = Mathf.Max(existing.TurnsLeft, newTurns);
        //       }
        //   }
        //
        // When you’re ready, we can implement that by extending StatusInstance and updating the UI (turns text stays the same).

        // REMEMBERME: apply new statuses here
        private void ApplyPostActionEffects(CombatActionId actionId, bool actorIsPlayer)
        {
            // Minimal hardcoded table (will become ScriptableObject/data later).
            switch (actionId)
            {
                case CombatActionId.FireSpell:
                {
                    // Fire applies burning to the target.
                    if (actorIsPlayer)
                        AddOrRefreshEnemyStatusInternal(StatusEffectId.Burning, FireSpellBurningTurns);
                    else
                        AddOrRefreshPlayerStatusInternal(StatusEffectId.Burning, FireSpellBurningTurns);
                    break;
                }

                case CombatActionId.Block:
                {
                    // Block applies a visible status on the actor.
                    // The actual armor mechanics are handled by CombatState in BattleCombatEngine.
                    if (actorIsPlayer)
                        SetStatusExact(playerStatuses, StatusEffectId.Block, BlockTurns);
                    else
                        SetStatusExact(enemyStatuses, StatusEffectId.Block, BlockTurns);
                    break;
                }
            }
        }
    }
}
