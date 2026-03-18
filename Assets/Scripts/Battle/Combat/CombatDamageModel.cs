namespace Game.Battle.Combat
{
    /// <summary>
    /// Shared damage model for combat resolution and tooltip previews.
    /// Keep formulas centralized so UI and runtime never diverge.
    /// </summary>
    public static class CombatDamageModel
    {
        public const int DefaultBaseAttack = 10;

        public static int NormalizeBaseAttack(int baseAttack)
        {
            return baseAttack < 0 ? 0 : baseAttack;
        }

        public static int ComputeHpDamage(int baseAttack, Actions.CombatActionData action)
        {
            if (action == null)
                return 0;

            var normalizedAttack = NormalizeBaseAttack(baseAttack);
            if (normalizedAttack <= 0 || action.HpDamageMultiplier <= 0f)
                return 0;

            var raw = normalizedAttack * (double)action.HpDamageMultiplier;
            var rounded = (int)System.Math.Round(raw, System.MidpointRounding.AwayFromZero);
            return rounded < 0 ? 0 : rounded;
        }

        public static int ComputeHpDamage(int physicalDamage, int magicDamage, Actions.CombatActionData action)
        {
            var selectedBaseAttack = SelectBaseAttack(physicalDamage, magicDamage, action);
            return ComputeHpDamage(selectedBaseAttack, action);
        }

        public static int SelectBaseAttack(int physicalDamage, int magicDamage, Actions.CombatActionData action)
        {
            if (action != null && action.Category == Actions.CombatActionCategory.Magic)
                return NormalizeBaseAttack(magicDamage);

            return NormalizeBaseAttack(physicalDamage);
        }

        public static int ComputeSelfHealPreviewFromHpDamage(
            Actions.CombatActionId actionId,
            int computedHpDamage,
            Actions.CombatActionData action)
        {
            if (actionId == Actions.CombatActionId.DarkSpell)
                return computedHpDamage > 0 ? computedHpDamage : 0;

            if (action == null || action.HpHealSelf <= 0)
                return 0;

            return action.HpHealSelf;
        }
    }
}
