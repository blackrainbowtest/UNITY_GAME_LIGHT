using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Game.Battle.Combat.Actions;
using Random = UnityEngine.Random;

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

        [Header("Presentation Catalog")]
        [SerializeField] private BattleOutcomePresentationCatalogAsset presentationCatalog;
        [SerializeField] private Image outcomeImage;
        [Tooltip("If assigned, animated variant prefabs will be instantiated under this root.")]
        [SerializeField] private Transform animatedVariantRoot;
        [SerializeField] private bool enablePresentationDebugLogs = true;
        [SerializeField] private bool showCharacterAnimationsAsStaticImages = true;
        [SerializeField] private Image playerImage;
        [SerializeField] private Image enemyImage;
        [Tooltip("Player animation target used to play selected player animation asset.")]
        [SerializeField] private BackgroundSpriteAnimationPlayer playerAnimationPlayer;
        [Tooltip("Enemy animation target used to play selected enemy animation asset.")]
        [SerializeField] private BackgroundSpriteAnimationPlayer enemyAnimationPlayer;

        private Action _onClosed;
        private bool _suppressHideOnAwake;
        private bool _isOpen;
        private bool _hideOnClose = true;
        private GameObject _spawnedAnimatedVariant;
        private Sprite _fallbackLocationBackground;

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

        public void Show(
            Game.Battle.BattleFinishReason reason,
            bool playerWon,
            CombatActionId? winningActionId,
            Action onClosed,
            bool hideOnClose = true,
            string enemyId = null,
            string locationId = null,
            string sourceLocationId = null,
            Sprite fallbackLocationBackground = null)
        {
            _onClosed = onClosed;
            _isOpen = true;
            _hideOnClose = hideOnClose;
            _fallbackLocationBackground = fallbackLocationBackground;
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

            ApplyPresentation(reason, enemyId, locationId, sourceLocationId);

            Debug.Log($"[BattleOutcomeAnimationModal] Show: reason={reason}, playerWon={playerWon}, winningAction={winningActionId}, root={(root != null ? root.name : "<self>")}", this);
        }

        public void Hide()
        {
            ClearSpawnedVariants();
            _fallbackLocationBackground = null;

            // Always hide the whole modal object to avoid leaving any overlay elements (e.g. TopHUD) active.
            if (root != null)
                root.SetActive(false);

            gameObject.SetActive(false);
        }

        private void Close()
        {
            FinalizeClose(from: "Close");

            if (_hideOnClose)
                Hide();
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

            if (animatedVariantRoot == null)
            {
                var content = transform.Find("contentHUD");
                if (content != null)
                    animatedVariantRoot = content;
            }

            if (playerAnimationPlayer == null)
            {
                var players = GetComponentsInChildren<BackgroundSpriteAnimationPlayer>(includeInactive: true);
                for (int i = 0; i < players.Length; i++)
                {
                    var p = players[i];
                    if (p == null)
                        continue;

                    if (p.name.IndexOf("player", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        playerAnimationPlayer = p;
                        break;
                    }
                }
            }

            if (enemyAnimationPlayer == null)
            {
                var players = GetComponentsInChildren<BackgroundSpriteAnimationPlayer>(includeInactive: true);
                for (int i = 0; i < players.Length; i++)
                {
                    var p = players[i];
                    if (p == null)
                        continue;

                    if (p.name.IndexOf("enemy", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        enemyAnimationPlayer = p;
                        break;
                    }
                }
            }

            if (outcomeImage == null)
            {
                var images = GetComponentsInChildren<Image>(includeInactive: true);
                for (int i = 0; i < images.Length; i++)
                {
                    var img = images[i];
                    if (img == null)
                        continue;

                    if (img == GetComponent<Image>())
                        continue;

                    if (img.name.IndexOf("outcome", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        img.name.IndexOf("result", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        img.name.IndexOf("image", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        outcomeImage = img;
                        break;
                    }
                }
            }

            if (playerImage == null)
            {
                var images = GetComponentsInChildren<Image>(includeInactive: true);
                for (int i = 0; i < images.Length; i++)
                {
                    var img = images[i];
                    if (img == null || img == outcomeImage || img == GetComponent<Image>())
                        continue;

                    if (img.name.IndexOf("player", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        playerImage = img;
                        break;
                    }
                }
            }

            if (enemyImage == null)
            {
                var images = GetComponentsInChildren<Image>(includeInactive: true);
                for (int i = 0; i < images.Length; i++)
                {
                    var img = images[i];
                    if (img == null || img == outcomeImage || img == GetComponent<Image>())
                        continue;

                    if (img.name.IndexOf("enemy", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        enemyImage = img;
                        break;
                    }
                }
            }

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

        private void ApplyPresentation(Game.Battle.BattleFinishReason reason, string enemyId, string locationId, string sourceLocationId)
        {
            if (presentationCatalog == null)
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                if (enablePresentationDebugLogs)
                    Debug.LogWarning("[BattleOutcomeAnimationModal] presentationCatalog is not assigned.", this);
#endif
                return;
            }

            var save = global::GameState.Instance?.CurrentSave;
            var resolvedEnemyId = !string.IsNullOrWhiteSpace(enemyId)
                ? enemyId
                : save?.sceneState?.pendingBattle?.enemyId;
            var resolvedLocationId = !string.IsNullOrWhiteSpace(locationId)
                ? locationId
                : save?.sceneState?.pendingBattle?.locationId;
            var resolvedSourceLocationId = !string.IsNullOrWhiteSpace(sourceLocationId)
                ? sourceLocationId
                : save?.sceneState?.pendingBattle?.locationId;
            var resolvedLocationFallbackSprite = presentationCatalog.ResolveLocationFallbackSprite(
                resolvedLocationId,
                resolvedSourceLocationId,
                _fallbackLocationBackground);

            var variants = presentationCatalog.ResolveVariants(
                reason,
                resolvedEnemyId,
                resolvedLocationId,
                resolvedSourceLocationId,
                save,
                debugLogs: enablePresentationDebugLogs,
                out var debugReport);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (enablePresentationDebugLogs && !string.IsNullOrWhiteSpace(debugReport))
                Debug.Log("[BattleOutcomeAnimationModal] Resolve debug:\n" + debugReport, this);
#endif

            if (variants == null || variants.Count == 0)
            {
                if (outcomeImage != null)
                {
                    outcomeImage.sprite = resolvedLocationFallbackSprite;
                    outcomeImage.enabled = outcomeImage.sprite != null;
                }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
                if (enablePresentationDebugLogs)
                    Debug.LogWarning($"[BattleOutcomeAnimationModal] No variants matched. reason={reason}, enemyId='{resolvedEnemyId}', locationId='{resolvedLocationId}', sourceLocationId='{resolvedSourceLocationId}'", this);
#endif
                return;
            }

            var selected = SelectWeightedVariant(variants);
            if (selected == null)
            {
                if (outcomeImage != null)
                {
                    outcomeImage.sprite = resolvedLocationFallbackSprite;
                    outcomeImage.enabled = outcomeImage.sprite != null;
                }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
                if (enablePresentationDebugLogs)
                    Debug.LogWarning("[BattleOutcomeAnimationModal] Variant list exists, but weighted selection returned null.", this);
#endif
                return;
            }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (enablePresentationDebugLogs)
            {
                Debug.Log(
                    $"[BattleOutcomeAnimationModal] Selected variant: id='{selected.id}', hasSprite={selected.sprite != null}, hasAnimatedPrefab={selected.animatedPrefab != null}, hasPlayerAnimation={selected.playerAnimation != null}, hasEnemyAnimation={selected.enemyAnimation != null}, weight={selected.weight}",
                    this);
            }
#endif

            if (outcomeImage != null)
            {
                var variantSprite = presentationCatalog.UseVariantSpriteOverrides ? selected.sprite : null;
                var resolvedSprite = variantSprite != null ? variantSprite : resolvedLocationFallbackSprite;
                outcomeImage.sprite = resolvedSprite;
                outcomeImage.enabled = resolvedSprite != null;
            }

            ClearSpawnedVariants();

            if (selected.animatedPrefab != null)
            {
                var parent = animatedVariantRoot != null ? animatedVariantRoot : transform;
                _spawnedAnimatedVariant = Instantiate(selected.animatedPrefab, parent, worldPositionStays: false);
            }

            if (showCharacterAnimationsAsStaticImages)
            {
                ApplyAnimationFirstFrameToImage(selected.playerAnimation, playerImage);
                ApplyAnimationFirstFrameToImage(selected.enemyAnimation, enemyImage);
            }
            else
            {
                if (selected.playerAnimation != null && playerAnimationPlayer != null)
                    playerAnimationPlayer.SetSingleAnimation(selected.playerAnimation, restartPlayback: true);

                if (selected.enemyAnimation != null && enemyAnimationPlayer != null)
                    enemyAnimationPlayer.SetSingleAnimation(selected.enemyAnimation, restartPlayback: true);
            }
        }

        private static BattleOutcomePresentationCatalogAsset.VisualVariant SelectWeightedVariant(IReadOnlyList<BattleOutcomePresentationCatalogAsset.VisualVariant> variants)
        {
            if (variants == null || variants.Count == 0)
                return null;

            var totalWeight = 0;
            for (int i = 0; i < variants.Count; i++)
            {
                var v = variants[i];
                if (v == null || (v.sprite == null && v.animatedPrefab == null && v.playerAnimation == null && v.enemyAnimation == null))
                    continue;

                totalWeight += Mathf.Max(1, v.weight);
            }

            if (totalWeight <= 0)
                return null;

            var roll = Random.Range(0, totalWeight);
            var cumulative = 0;
            for (int i = 0; i < variants.Count; i++)
            {
                var v = variants[i];
                if (v == null || (v.sprite == null && v.animatedPrefab == null && v.playerAnimation == null && v.enemyAnimation == null))
                    continue;

                cumulative += Mathf.Max(1, v.weight);
                if (roll < cumulative)
                    return v;
            }

            return variants[0];
        }

        private void ClearSpawnedVariants()
        {
            if (_spawnedAnimatedVariant != null)
            {
                Destroy(_spawnedAnimatedVariant);
                _spawnedAnimatedVariant = null;
            }

            if (showCharacterAnimationsAsStaticImages)
            {
                if (playerImage != null)
                {
                    playerImage.sprite = null;
                    playerImage.enabled = false;
                }

                if (enemyImage != null)
                {
                    enemyImage.sprite = null;
                    enemyImage.enabled = false;
                }
            }
            else
            {
                if (playerAnimationPlayer != null)
                    playerAnimationPlayer.SetSingleAnimation(null, restartPlayback: false);

                if (enemyAnimationPlayer != null)
                    enemyAnimationPlayer.SetSingleAnimation(null, restartPlayback: false);
            }
        }

        private static void ApplyAnimationFirstFrameToImage(IdleAnimation animation, Image target)
        {
            if (target == null)
                return;

            if (animation == null || animation.FramesArray == null || animation.FramesArray.Length == 0)
            {
                target.sprite = null;
                target.enabled = false;
                return;
            }

            target.sprite = animation.FramesArray[0];
            target.enabled = target.sprite != null;
        }
    }
}
