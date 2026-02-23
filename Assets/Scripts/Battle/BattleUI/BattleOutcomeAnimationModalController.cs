using System;
using UnityEngine;
using UnityEngine.UI;
using Game.Battle.Combat.Actions;

namespace Game.Battle.UI
{
    /// <summary>
    /// Full-screen modal shown before battle results for outcomes that should play an animation sequence.
    /// For now it's a placeholder: only a close (X) button is required.
    /// </summary>
    public sealed class BattleOutcomeAnimationModalController : MonoBehaviour
    {
        [Header("Wiring")]
        [SerializeField] private GameObject root;
        [SerializeField] private Button closeButton;

        private Action _onClosed;
        private bool _suppressHideOnAwake;
        private bool _isOpen;

        public Game.Battle.BattleFinishReason LastReason { get; private set; } = Game.Battle.BattleFinishReason.Defeat;
        public bool LastPlayerWon { get; private set; }
        public CombatActionId? LastWinningActionId { get; private set; }

        private void Awake()
        {
            AutoWireIfMissing();

            if (closeButton != null)
                closeButton.onClick.AddListener(Close);

            if (!_suppressHideOnAwake)
                Hide();
        }

        private void OnDisable()
        {
            // If someone closes/disables this modal via a UnityEvent (without calling Close()),
            // we still must continue the battle finish flow.
            if (_isOpen)
            {
                FinalizeClose(from: "OnDisable");
            }
        }

        public void Show(Game.Battle.BattleFinishReason reason, bool playerWon, CombatActionId? winningActionId, Action onClosed)
        {
            _onClosed = onClosed;
            _isOpen = true;
            LastReason = reason;
            LastPlayerWon = playerWon;
            LastWinningActionId = winningActionId;

            // Ensure the hierarchy is active even if this object was disabled.
            _suppressHideOnAwake = true;
            gameObject.SetActive(true);
            _suppressHideOnAwake = false;

            // Bring to front for UI so it isn't hidden behind other panels.
            transform.SetAsLastSibling();

            if (root != null)
                root.SetActive(true);
            else
                gameObject.SetActive(true);

            Debug.Log($"[BattleOutcomeAnimationModal] Show: reason={reason}, playerWon={playerWon}, winningAction={winningActionId}, root={(root != null ? root.name : "<self>")}", this);
        }

        public void Hide()
        {
            // Always hide the whole modal object to avoid leaving any overlay elements (e.g. TopHUD) active.
            if (root != null)
                root.SetActive(false);

            gameObject.SetActive(false);
        }

        private void Close()
        {
            FinalizeClose(from: "Close");
        }

        private void FinalizeClose(string from)
        {
            if (!_isOpen)
                return;

            _isOpen = false;

            var cb = _onClosed;
            _onClosed = null;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[BattleOutcomeAnimationModal] FinalizeClose from={from}, hasCallback={(cb != null)}", this);
#endif

            cb?.Invoke();
        }

        private void AutoWireIfMissing()
        {
            if (root == null)
                root = gameObject;

            if (closeButton == null)
            {
                // Try to find a close button by name.
                var buttons = GetComponentsInChildren<Button>(true);
                for (int i = 0; i < buttons.Length; i++)
                {
                    var b = buttons[i];
                    if (b == null) continue;

                    var n = b.name;
                    if (n.IndexOf("close", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        n.Equals("x", StringComparison.OrdinalIgnoreCase) ||
                        n.IndexOf("exit", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        closeButton = b;
                        break;
                    }
                }
            }
        }
    }
}
