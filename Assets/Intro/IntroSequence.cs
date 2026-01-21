  ____  ____  _     ___ _____ ____  _   _    _    ____  ____    ____ _____ _   _ ____ ___ ___  
 / ___||  _ \| |   |_ _|_   _/ ___|| | | |  / \  |  _ \|  _ \  / ___|_   _| | | |  _ \_ _/ _ \ 
 \___ \| |_) | |    | |  | | \___ \| |_| | / _ \ | |_) | | | | \___ \ | | | | | | | | | | | | |
  ___) |  __/| |___ | |  | |  ___) |  _  |/ ___ \|  _ <| |_| |  ___) || | | |_| | |_| | | |_| |
 |____/|_|   |_____|___| |_| |____/|_| |_/_/   \_\_| \_\____/  |____/ |_|  \___/|____/___\___/ 

/* ******************************************************************************************************** */
/*                                                                                                          */
/*   File: Assets/Intro/IntroSequence.cs                                                                    */
/*                                                        /\_/\                                             */
/*                                                       ( •.• )                                            */
/*   By: unluckydungeonadventure.gmail.com                > ^ <                                             */
/*                                                                                                          */
/*   Created: 2026/01/21 16:08:23 by UDA                                                                    */
/*   Updated: 2026/01/21 16:08:23 by UDA                                                                    */
/*                                                                                                          */
/* ******************************************************************************************************** */

using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Defines an intro sequence as an ordered list of frames.
/// 
/// This asset represents declarative data only.
/// All modifications must go through explicit methods
/// to preserve sequence integrity.
/// </summary>
[CreateAssetMenu(
    fileName = "IntroSequence",
    menuName = "Intro/IntroSequence",
    order = 2
)]
public class IntroSequence : ScriptableObject
{
    [SerializeField]
    private List<IntroFrame> frames = new();

    /// <summary>
    /// Read-only access to intro frames.
    /// </summary>
    public IReadOnlyList<IntroFrame> Frames => frames;

#if UNITY_EDITOR
    /// <summary>
    /// Editor-only method used to replace the entire sequence.
    /// 
    /// This is intended for importers and editor tools only.
    /// Runtime code must not mutate intro data.
    /// </summary>
    public void EditorSetFrames(List<IntroFrame> newFrames)
    {
        // TODO:
        // Add validation if intro sequencing rules become more complex
        // (e.g. required first/last frame types).
        frames = newFrames;
    }
#endif
}
