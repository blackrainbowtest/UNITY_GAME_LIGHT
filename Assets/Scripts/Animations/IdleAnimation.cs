/* ************************************************************************** */
/*                                                                            */
/*   File: Assets/Scripts/Animations/IdleAnimation.cs                         */
/*                                                        /\_/\               */
/*                                                       ( •.• )              */
/*   By: unluckydungeonadventure.gmail.com                > ^ <               */
/*                                                                            */
/*   Created: 2026/01/08 10:09:21 by UDA                                      */
/*   Updated: 2026/01/08 10:09:21 by UDA                                      */
/*                                                                            */
/* ************************************************************************** */

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

    // Unique identifier used by animation systems
    public string Id => id;

    // Read-only access to animation frames
    public IReadOnlyList<Sprite> Frames => frames;

    // Frames per second used for playback
    public float FrameRate => frameRate;

    public int FrameCount => frames != null ? frames.Length : 0;

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
