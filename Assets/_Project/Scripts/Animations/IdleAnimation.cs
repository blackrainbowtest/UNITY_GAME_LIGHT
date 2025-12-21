using UnityEngine;

[CreateAssetMenu(fileName = "IdleAnimation", menuName = "Animations/IdleAnimation", order = 1)]
public class IdleAnimation : ScriptableObject
{
    public string id = "idle_01";
    public Sprite[] frames;
    public float frameRate = 12f;
}
