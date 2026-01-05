using UnityEngine;

[CreateAssetMenu(fileName = "IntroFrame", menuName = "Intro/IntroFrame", order = 1)]
public class IntroFrame : ScriptableObject
{
    public string id;
    public string textKey;
    public Sprite background;
    public VoiceType voiceType;
    public bool waitForClick = true;
    public float autoDelay = 0f;
}

public enum VoiceType
{
    None,
    Narrator,
    Character,
    Tutorial
}
