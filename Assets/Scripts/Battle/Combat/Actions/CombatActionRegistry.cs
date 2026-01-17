using System.Collections.Generic;

namespace Game.Battle.Combat.Actions
{
    /// <summary>
    /// Central registry of all combat actions.
    /// Single source of truth for action data.
    /// </summary>
    public sealed class CombatActionRegistry
    {
        private readonly Dictionary<CombatActionId, CombatActionData> _actions;

        public CombatActionRegistry()
        {
            _actions = new Dictionary<CombatActionId, CombatActionData>
            {
                {
                    CombatActionId.FastAttack,
                    new CombatActionData(
                        id: CombatActionId.FastAttack,
                        category: CombatActionCategory.Attack,
                        hpDamage: 10,
                        mpCost: 0,
                        spCost: 5,
                        lpCost: 0,
                        requiresPlayerBlockedLastTurn: false)
                },
                {
                    CombatActionId.NormalAttack,
                    new CombatActionData(
                        CombatActionId.NormalAttack,
                        CombatActionCategory.Attack,
                        hpDamage: 18,
                        mpCost: 0,
                        spCost: 10,
                        lpCost: 0,
                        requiresPlayerBlockedLastTurn: false)
                },
                {
                    CombatActionId.HeavyAttack,
                    new CombatActionData(
                        CombatActionId.HeavyAttack,
                        CombatActionCategory.Attack,
                        hpDamage: 30,
                        mpCost: 0,
                        spCost: 20,
                        lpCost: 0,
                        requiresPlayerBlockedLastTurn: false)
                },
                {
                    CombatActionId.CounterAttack,
                    new CombatActionData(
                        CombatActionId.CounterAttack,
                        CombatActionCategory.Attack,
                        hpDamage: 45,
                        mpCost: 0,
                        spCost: 8,
                        lpCost: 0,
                        requiresPlayerBlockedLastTurn: true)
                },
                {
                    CombatActionId.Block,
                    new CombatActionData(
                        CombatActionId.Block,
                        CombatActionCategory.Defense,
                        hpDamage: 0,
                        mpCost: 0,
                        spCost: 5,
                        lpCost: 0,
                        requiresPlayerBlockedLastTurn: false)
                }
            };
        }

        public CombatActionData Get(CombatActionId id)
        {
            return _actions.TryGetValue(id, out var action)
                ? action
                : null;
        }

        public IEnumerable<CombatActionData> GetByCategory(CombatActionCategory category)
        {
            foreach (var action in _actions.Values)
            {
                if (action.Category == category)
                    yield return action;
            }
        }
    }
}
