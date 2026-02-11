//   ____  ____  _     ___ _____ ____  _   _    _    ____  ____    ____ _____ _   _ ____ ___ ___  
//  / ___||  _ \| |   |_ _|_   _/ ___|| | | |  / \  |  _ \|  _ \  / ___|_   _| | | |  _ \_ _/ _ \ 
//  \___ \| |_) | |    | |  | | \___ \| |_| | / _ \ | |_) | | | | \___ \ | | | | | | | | | | | | |
//   ___) |  __/| |___ | |  | |  ___) |  _  |/ ___ \|  _ <| |_| |  ___) || | | |_| | |_| | | |_| |
//  |____/|_|   |_____|___| |_| |____/|_| |_/_/   \_\_| \_\____/  |____/ |_|  \___/|____/___\___/ 
//
/* ******************************************************************************************************** */
/*                                                                                                          */
/*   File: Assets\Scripts\Animations\IdleAnimation.cs                                                                              */
/*                                                        /\_/\                                             */
/*                                                       ( •.• )                                            */
/*   By: unluckydungeonadventure.gmail.com                > ^ <                                             */
/*                                                                                                          */
/*   Created: 2026/01/08 10:09:21 by UDA                                                                    */
/*   Updated: 2026/01/23 01:36:41 by UDA                                                                    */
/*                                                                                                          */
/* ******************************************************************************************************** */

using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(
    fileName = "IdleAnimation",
    menuName = "Animations/IdleAnimation",
    order = 1
)]
public class IdleAnimation : ScriptableObject
{
    private const float DefaultFrameRate = 12f;

    [SerializeField] private string id = "idle_01";
    [SerializeField] private Sprite[] frames;
    [SerializeField] private float frameRate = DefaultFrameRate;

    [Header("Impact (Optional)")]
    [Tooltip("If enabled, animation systems may trigger an impact callback exactly once when playback reaches the configured impact moment.")]
    [SerializeField] private bool hasImpact;

    [Tooltip("Preferred. Normalized impact time (0..1). Used when Impact Frame Index is not set.")]
    [SerializeField, Range(0f, 1f)] private float impactTimeNormalized = 0.5f;

    [Tooltip("Optional override. 1-based frame index (1 = first frame). If > 0, overrides normalized time.")]
    [SerializeField] private int impactFrameIndex = -1;

    // Unique identifier used by animation systems
    public string Id => id;

    // Read-only access to animation frames
    public IReadOnlyList<Sprite> Frames => frames;

    // Direct access for runtime systems that require arrays (no allocations)
    public Sprite[] FramesArray => frames;

    // Frames per second used for playback
    public float FrameRate => frameRate;

    public int FrameCount => frames != null ? frames.Length : 0;

    public bool HasImpact => hasImpact;

    public float ImpactTimeNormalized => impactTimeNormalized;

    public int ImpactFrameIndex => impactFrameIndex;

    public bool TryGetImpactFrameIndex(out int frameIndex1Based)
    {
        frameIndex1Based = -1;

        if (!hasImpact)
            return false;

        if (frames == null || frames.Length == 0)
            return false;

        if (impactFrameIndex > 0)
        {
            frameIndex1Based = Mathf.Clamp(impactFrameIndex, 1, frames.Length);
            return true;
        }

        // Map normalized time to a discrete frame index (1..N).
        var t = Mathf.Clamp01(impactTimeNormalized);
        var idx0 = Mathf.RoundToInt((frames.Length - 1) * t);
        frameIndex1Based = Mathf.Clamp(idx0 + 1, 1, frames.Length);
        return true;
    }

    // Validates animation data consistency
    public bool IsValid()
    {
        return frames != null && frames.Length > 0 && frameRate > 0f;
    }

#if UNITY_EDITOR
    // Editor-only entry point for replacing animation frames
    // Centralizes validation and protects data integrity
    public void EditorSetFrames(Sprite[] newFrames)
    {
        if (newFrames == null || newFrames.Length == 0)
        {
            Debug.LogError("IdleAnimation: Attempted to assign empty frame list.");
            return;
        }

        frames = newFrames;
    }
#endif
}
