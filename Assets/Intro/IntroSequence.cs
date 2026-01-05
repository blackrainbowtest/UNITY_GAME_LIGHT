using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "IntroSequence", menuName = "Intro/IntroSequence", order = 2)]
public class IntroSequence : ScriptableObject
{
    public List<IntroFrame> frames;
}
