using System;
using UnityEngine;

[Serializable]
public class IntroStepData
{
    [Header("Visual")]
    public Sprite background;

    [Header("Text")]
    public LocalizedText text;

    [Header("Flags")]
    public bool isFinalStep;
}
