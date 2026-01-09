using UnityEngine;

public enum ItemType
{
    Currency,
    Consumable
}

public enum ConsumableEffect
{
    HealHP,
    RestoreMana,
    RestoreStamina
}

[CreateAssetMenu(menuName = "Game/Item")]
public class ItemDefinition : ScriptableObject
{
    [Header("Identity")]
    [SerializeField] private string id;
    [SerializeField] private ItemType type;

    [Header("UI")]
    [SerializeField] private string displayName;
    [SerializeField] private Sprite icon;

    [Header("Consumable")]
    [SerializeField] private ConsumableEffect effect;
    [SerializeField] private int value;

    public string Id => id;
    public ItemType Type => type;
    public string DisplayName => displayName;
    public Sprite Icon => icon;
    public ConsumableEffect Effect => effect;
    public int Value => value;
}
