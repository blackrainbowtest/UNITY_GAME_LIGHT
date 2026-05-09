using UnityEngine;
using Game.Battle.Combat;

namespace Game.Battle
{
    public partial class BattleController
    {
        private float _battleStartedAtRealtime;
        private int _playerHpDamageDealtThisBattle;
        private int _playerHpDamageTakenThisBattle;
        private int _playerLpDamageDealtThisBattle;
        private int _playerLpDamageTakenThisBattle;

        private void ResetBattleSummaryTracking()
        {
            _battleStartedAtRealtime = Time.realtimeSinceStartup;
            _playerHpDamageDealtThisBattle = 0;
            _playerHpDamageTakenThisBattle = 0;
            _playerLpDamageDealtThisBattle = 0;
            _playerLpDamageTakenThisBattle = 0;
        }

        private void AccumulateBattleSummaryDelta(CombatState before, CombatState after)
        {
            if (before == null || after == null)
                return;

            _playerHpDamageDealtThisBattle += Mathf.Max(0, before.EnemyHp - after.EnemyHp);
            _playerHpDamageTakenThisBattle += Mathf.Max(0, before.PlayerHp - after.PlayerHp);

            // LP in this project grows when lust damage is received.
            _playerLpDamageDealtThisBattle += Mathf.Max(0, after.EnemyLp - before.EnemyLp);
            _playerLpDamageTakenThisBattle += Mathf.Max(0, after.PlayerLp - before.PlayerLp);
        }

        private int ResolveBattleDurationSeconds()
        {
            if (_battleStartedAtRealtime <= 0f)
                return 0;

            return Mathf.Max(0, Mathf.RoundToInt(Time.realtimeSinceStartup - _battleStartedAtRealtime));
        }
    }
}
