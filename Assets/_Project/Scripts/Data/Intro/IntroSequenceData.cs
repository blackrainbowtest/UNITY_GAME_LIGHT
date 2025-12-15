using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "IntroSequence",
    menuName = "Game/Intro/Intro Sequence"
)]
public class IntroSequenceData : ScriptableObject
{
    public string id;
    public List<IntroStepData> steps = new List<IntroStepData>();
    [Tooltip("Сохранять ли прогресс после завершения интро")]
    public bool autoSaveAfterFinish = true;
}
